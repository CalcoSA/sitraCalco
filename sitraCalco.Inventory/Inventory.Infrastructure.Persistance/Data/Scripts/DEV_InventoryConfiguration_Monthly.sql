-- EJECUCIÓN MANUAL EXCLUSIVA EN DEV. Este archivo no se ejecuta desde la aplicación.
-- Ejecutar primero el bloque de diagnóstico, revisar sus resultados y hacer respaldo.
-- Detener Inventory y TODOS los demás escritores/eventos durante TODO el procedimiento.
-- Usar una cuenta DBA con visibilidad de INFORMATION_SCHEMA en todos los esquemas.
-- Las referencias lógicas sin FK y con nombres distintos requieren revisión manual.
-- Nunca borrar inventarios/conteos/reconteos/conciliaciones para superar una validación.
-- ALTER TABLE hace COMMIT implícito: la limpieza DML es atómica, pero no comparte
-- transacción con el DDL. Si falla el ALTER, los datos de prueba ya estarán eliminados.
-- No ejecutar desde una sesión que tenga otra transacción abierta.

-- 1. DIAGNÓSTICO (seleccionar previamente el esquema DEV correcto con USE).
SELECT DATABASE() AS selected_database, VERSION() AS mysql_version;
SELECT k.CONSTRAINT_SCHEMA, k.TABLE_NAME, k.COLUMN_NAME, k.CONSTRAINT_NAME,
       k.REFERENCED_TABLE_SCHEMA, k.REFERENCED_TABLE_NAME, k.REFERENCED_COLUMN_NAME,
       r.DELETE_RULE, r.UPDATE_RULE
FROM information_schema.KEY_COLUMN_USAGE k
JOIN information_schema.REFERENTIAL_CONSTRAINTS r
  ON r.CONSTRAINT_SCHEMA = k.CONSTRAINT_SCHEMA AND r.CONSTRAINT_NAME = k.CONSTRAINT_NAME
 AND r.TABLE_NAME = k.TABLE_NAME
WHERE k.REFERENCED_TABLE_SCHEMA = DATABASE()
  AND k.REFERENCED_TABLE_NAME IN ('sitracalco_inventory_configuration',
      'sitracalco_inventory_configuration_assignment', 'sitracalco_inventory_configuration_day')
ORDER BY k.CONSTRAINT_SCHEMA, k.TABLE_NAME, k.CONSTRAINT_NAME, k.ORDINAL_POSITION;

SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND (COLUMN_NAME IN ('inventory_configuration_id', 'inventory_configuration_assignment_id',
       'inventory_configuration_day_id') OR TABLE_NAME = 'sitracalco_inventory_configuration')
ORDER BY TABLE_NAME, ORDINAL_POSITION;

SELECT TRIGGER_SCHEMA, TRIGGER_NAME, EVENT_OBJECT_TABLE, ACTION_STATEMENT
FROM information_schema.TRIGGERS
WHERE EVENT_OBJECT_SCHEMA = DATABASE()
  AND (EVENT_OBJECT_TABLE IN ('sitracalco_inventory_configuration',
       'sitracalco_inventory_configuration_assignment', 'sitracalco_inventory_configuration_day')
       OR ACTION_STATEMENT LIKE '%sitracalco_inventory_configuration%');
SELECT TABLE_SCHEMA, TABLE_NAME, VIEW_DEFINITION
FROM information_schema.VIEWS
WHERE VIEW_DEFINITION LIKE '%sitracalco_inventory_configuration%';
SELECT ROUTINE_SCHEMA, ROUTINE_NAME, ROUTINE_DEFINITION
FROM information_schema.ROUTINES
WHERE ROUTINE_DEFINITION LIKE '%sitracalco_inventory_configuration%';
SELECT EVENT_SCHEMA, EVENT_NAME, STATUS, EVENT_DEFINITION
FROM information_schema.EVENTS
WHERE EVENT_DEFINITION LIKE '%sitracalco_inventory_configuration%';

SELECT 'configuration' AS source_table, COUNT(*) AS rows_to_review FROM sitracalco_inventory_configuration
UNION ALL SELECT 'configuration_assignment', COUNT(*) FROM sitracalco_inventory_configuration_assignment
UNION ALL SELECT 'configuration_day', COUNT(*) FROM sitracalco_inventory_configuration_day
UNION ALL SELECT 'inventory (NO SE BORRA)', COUNT(*) FROM sitracalco_inventory_inventory;

-- 2. Tras revisar el diagnóstico, completar estos valores MANUALMENTE.
-- La ejecución sin editar se detiene ANTES de eliminar datos.
SET @inventory_dev_database = NULL; -- Nombre exacto del esquema DEV revisado.
SET @inventory_dev_dependencies_reviewed = 0; -- 1 sólo tras revisar referencias reales, incluidas las lógicas.
SET @inventory_dev_writers_stopped = 0; -- 1 sólo con todos los escritores detenidos.
SET @inventory_dev_delete_test_data = 0; -- 1 autoriza eliminar ÚNICAMENTE las tres tablas indicadas.

DELIMITER $$
CREATE PROCEDURE dev_migrate_inventory_configuration_monthly()
SQL SECURITY INVOKER
BEGIN
    DECLARE v_major INT;
    DECLARE v_minor INT;
    DECLARE v_patch INT;
    DECLARE v_message VARCHAR(128);
    DECLARE v_blocker VARCHAR(128);
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF DATABASE() IS NULL OR @inventory_dev_database IS NULL
       OR BINARY DATABASE() <> BINARY @inventory_dev_database
       OR COALESCE(@inventory_dev_dependencies_reviewed, 0) <> 1
       OR COALESCE(@inventory_dev_writers_stopped, 0) <> 1
       OR COALESCE(@inventory_dev_delete_test_data, 0) <> 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Detenido: confirmar DEV, revisar dependencias, detener escritores y autorizar datos de prueba.';
    END IF;
    IF VERSION() LIKE '%MariaDB%' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Este script requiere MySQL; revisar una migracion especifica para MariaDB.';
    END IF;

    -- Sólo se admite el esquema anterior completo. No repetir ni aplicar a un estado parcial.
    IF (SELECT COUNT(*) FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'sitracalco_inventory_configuration') <> 4
       OR (SELECT COUNT(*) FROM information_schema.COLUMNS
           WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'sitracalco_inventory_configuration'
             AND COLUMN_NAME IN ('inventory_configuration_id', 'inventory_configuration_name', 'start_date', 'end_date')) <> 4 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Esquema inesperado o migracion ya aplicada. Revisar columnas antes de continuar.';
    END IF;
    IF (SELECT COUNT(*) FROM information_schema.TABLES
        WHERE TABLE_SCHEMA = DATABASE() AND ENGINE = 'InnoDB' AND TABLE_TYPE = 'BASE TABLE'
          AND TABLE_NAME IN ('sitracalco_inventory_configuration', 'sitracalco_inventory_configuration_assignment',
              'sitracalco_inventory_configuration_day', 'sitracalco_inventory_inventory')) <> 4 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Falta una tabla esperada o no usa InnoDB. No se puede garantizar la limpieza transaccional.';
    END IF;
    IF @@SESSION.foreign_key_checks <> 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'La sesion debe tener habilitada la comprobacion de claves foraneas.';
    END IF;

    -- Referencia lógica histórica conocida: bloquear incluso si no existe FK física.
    -- Se exige tabla histórica vacía; no se borran ni siquiera históricos huérfanos.
    IF EXISTS (SELECT 1 FROM sitracalco_inventory_inventory LIMIT 1) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Hay inventarios/conteos historicos. Detenido SIN borrarlos; revisar una estrategia de migracion con datos.';
    END IF;

    -- Revisar todas las FK entrantes, también desde otros esquemas y hacia las tablas hijas.
    -- Sólo se permiten las dos referencias conocidas de hijos a configuration(id).
    -- Una FK adicional bloquea aunque su tabla esté vacía: necesita revisión explícita.
    SET v_blocker = (SELECT CONCAT(k.TABLE_SCHEMA, '.', k.TABLE_NAME)
        FROM information_schema.KEY_COLUMN_USAGE k
        WHERE k.REFERENCED_TABLE_SCHEMA = DATABASE()
          AND k.REFERENCED_TABLE_NAME IN ('sitracalco_inventory_configuration',
              'sitracalco_inventory_configuration_assignment', 'sitracalco_inventory_configuration_day')
          AND NOT (k.TABLE_SCHEMA = DATABASE()
              AND k.TABLE_NAME IN ('sitracalco_inventory_configuration_assignment', 'sitracalco_inventory_configuration_day')
              AND k.COLUMN_NAME = 'inventory_configuration_id'
              AND k.REFERENCED_TABLE_NAME = 'sitracalco_inventory_configuration'
              AND k.REFERENCED_COLUMN_NAME = 'inventory_configuration_id')
        LIMIT 1);
    IF v_blocker IS NOT NULL THEN
        SET v_message = LEFT(CONCAT('FK adicional: ', v_blocker, '. Revisar; no se elimina ninguna dependencia.'), 128);
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_message;
    END IF;

    -- Posibles dependencias sin FK (conteos, detalles, conciliaciones u otras tablas).
    SET v_blocker = (SELECT c.TABLE_NAME FROM information_schema.COLUMNS c
        WHERE c.TABLE_SCHEMA = DATABASE()
          AND c.COLUMN_NAME IN ('inventory_configuration_id', 'inventory_configuration_assignment_id', 'inventory_configuration_day_id')
          AND c.TABLE_NAME NOT IN ('sitracalco_inventory_configuration', 'sitracalco_inventory_configuration_assignment',
              'sitracalco_inventory_configuration_day', 'sitracalco_inventory_inventory')
        LIMIT 1);
    IF v_blocker IS NOT NULL THEN
        SET v_message = LEFT(CONCAT('Posible dependencia logica: ', v_blocker, '. Revisar antes de borrar.'), 128);
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = v_message;
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.TRIGGERS
        WHERE EVENT_OBJECT_SCHEMA = DATABASE() AND EVENT_OBJECT_TABLE IN ('sitracalco_inventory_configuration',
            'sitracalco_inventory_configuration_assignment', 'sitracalco_inventory_configuration_day')) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Hay triggers en las tablas a limpiar. Revisar sus efectos antes de continuar.';
    END IF;
    IF EXISTS (SELECT 1 FROM information_schema.VIEWS
        WHERE TABLE_SCHEMA = DATABASE() AND VIEW_DEFINITION LIKE '%sitracalco_inventory_configuration%') THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Hay vistas dependientes de configuracion. Revisarlas antes de eliminar columnas.';
    END IF;

    -- 3. ÚNICA limpieza autorizada; sin cascadas externas, TRUNCATE ni reinicio de IDs.
    START TRANSACTION;
    DELETE FROM sitracalco_inventory_configuration_assignment;
    DELETE FROM sitracalco_inventory_configuration_day;
    DELETE FROM sitracalco_inventory_configuration;
    COMMIT;

    -- 4. Cambio definitivo: tabla vacía, columnas nuevas obligatorias, sin fechas antiguas.
    SET v_major = CAST(SUBSTRING_INDEX(VERSION(), '.', 1) AS UNSIGNED);
    SET v_minor = CAST(SUBSTRING_INDEX(SUBSTRING_INDEX(VERSION(), '.', 2), '.', -1) AS UNSIGNED);
    SET v_patch = CAST(SUBSTRING_INDEX(SUBSTRING_INDEX(VERSION(), '.', 3), '.', -1) AS UNSIGNED);
    SET @inventory_dev_ddl = 'ALTER TABLE sitracalco_inventory_configuration
        DROP COLUMN start_date, DROP COLUMN end_date,
        ADD COLUMN start_day TINYINT UNSIGNED NOT NULL,
        ADD COLUMN end_day TINYINT UNSIGNED NOT NULL';
    -- MySQL >= 8.0.16 aplica CHECK; versiones anteriores no lo garantizan.
    IF v_major > 8 OR (v_major = 8 AND (v_minor > 0 OR v_patch >= 16)) THEN
        SET @inventory_dev_ddl = CONCAT(@inventory_dev_ddl,
            ', ADD CONSTRAINT ck_inventory_configuration_monthly_days CHECK (start_day BETWEEN 1 AND 31 AND end_day BETWEEN 1 AND 31 AND start_day <= end_day)');
    ELSE
        SELECT 'Esta version no aplica CHECK: la validacion queda en la API.' AS warning;
    END IF;
    PREPARE inventory_monthly_ddl FROM @inventory_dev_ddl;
    EXECUTE inventory_monthly_ddl;
    DEALLOCATE PREPARE inventory_monthly_ddl;
    SET @inventory_dev_ddl = NULL;

    -- 5. Verificación del resultado antes de habilitar la aplicación nueva.
    IF (SELECT COUNT(*) FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'sitracalco_inventory_configuration'
          AND COLUMN_NAME IN ('start_day', 'end_day') AND DATA_TYPE = 'tinyint'
          AND COLUMN_TYPE LIKE '%unsigned' AND IS_NULLABLE = 'NO') <> 2
       OR EXISTS (SELECT 1 FROM information_schema.COLUMNS
           WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'sitracalco_inventory_configuration'
             AND COLUMN_NAME IN ('start_date', 'end_date')) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Verificacion de esquema fallida. Mantener la aplicacion detenida y revisar el DDL.';
    END IF;
    SELECT 'Migracion DEV terminada. Crear nuevas configuraciones con startDay y endDay.' AS result;
END$$
DELIMITER ;

CALL dev_migrate_inventory_configuration_monthly();
DROP PROCEDURE dev_migrate_inventory_configuration_monthly;

SHOW CREATE TABLE sitracalco_inventory_configuration;
SELECT COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'sitracalco_inventory_configuration'
ORDER BY ORDINAL_POSITION;
SELECT COUNT(*) AS remaining_configurations FROM sitracalco_inventory_configuration;
-- Si CALL falla y el cliente se detiene, el procedimiento permanece: revisar el error
-- y eliminar manualmente SÓLO el procedimiento antes de volver a preparar el script.
