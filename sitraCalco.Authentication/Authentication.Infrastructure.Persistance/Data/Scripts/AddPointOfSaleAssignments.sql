-- Ejecutar manualmente en MySQL Workbench, con el esquema de Authentication seleccionado.
-- Una sola ejecución, antes de desplegar la API. No inserta roles, usuarios ni catálogo.
-- Suspender las escrituras administrativas durante la migración y el despliegue de todas las instancias.
-- Requiere permisos de DDL y CREATE ROUTINE / ALTER ROUTINE para el procedimiento temporal.
-- El DDL de MySQL hace commits implícitos: ante un error, revisar el esquema antes de reintentar.
-- El procedimiento temporal permite detener el script con un error si no se cumplen las precondiciones.

DELIMITER $$
CREATE PROCEDURE migrate_authentication_pdv_assignment()
BEGIN
    DECLARE current_role_name VARCHAR(100);
    DECLARE EXIT HANDLER FOR SQLEXCEPTION
    BEGIN
        ROLLBACK;
        RESIGNAL;
    END;

    IF NOT EXISTS (
        SELECT 1 FROM `sitracalco_authentication_role`
        WHERE `IdRole` = 6 AND BINARY `nameRole` = BINARY 'PDV'
    ) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'Se requiere el rol existente IdRole=6 con nombre exacto PDV.';
    END IF;

    IF EXISTS (SELECT 1 FROM `sitracalco_authentication_userrole` WHERE `IdRole` = 6) THEN
        SIGNAL SQLSTATE '45000'
            SET MESSAGE_TEXT = 'El rol PDV ya tiene usuarios; revisar sus asignaciones antes de migrar.';
    END IF;

    -- 1. Catálogo propio; sus IDs son internos, sin relación con Inventory.
    CREATE TABLE `sitracalco_authentication_solution_center` (
        `solution_center_id` INT NOT NULL AUTO_INCREMENT,
        `solution_center_code` VARCHAR(64) NOT NULL,
        `solution_center_name` VARCHAR(200) NOT NULL,
        PRIMARY KEY (`solution_center_id`),
        UNIQUE KEY `uq_auth_solution_center_code` (`solution_center_code`)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

    -- 2. Los roles existentes quedan con NULL hasta la actualización controlada final.
    ALTER TABLE `sitracalco_authentication_role`
        ADD COLUMN `role_code` VARCHAR(32) CHARACTER SET ascii COLLATE ascii_bin NULL DEFAULT NULL,
        ADD UNIQUE KEY `uq_auth_role_code` (`role_code`);

    -- 3. Se conservan los índices existentes. No se agrega UNIQUE sobre IdUser ni sobre el PDV.
    ALTER TABLE `sitracalco_authentication_userrole`
        ADD COLUMN `solution_center_id` INT NULL DEFAULT NULL,
        ADD KEY `ix_auth_userrole_solution_center` (`solution_center_id`),
        ADD CONSTRAINT `fk_auth_userrole_solution_center`
            FOREIGN KEY (`solution_center_id`)
            REFERENCES `sitracalco_authentication_solution_center` (`solution_center_id`)
            ON DELETE RESTRICT ON UPDATE RESTRICT;

    -- 4. Clasificar solamente el rol ya existente. No cambia su nombre, estado ni menús.
    START TRANSACTION;
    SELECT `nameRole` INTO current_role_name
        FROM `sitracalco_authentication_role` WHERE `IdRole` = 6 FOR UPDATE;

    IF current_role_name IS NULL OR BINARY current_role_name <> BINARY 'PDV' THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'El rol PDV cambió durante la migración.';
    END IF;
    IF EXISTS (SELECT 1 FROM `sitracalco_authentication_userrole` WHERE `IdRole` = 6) THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'Se asignaron usuarios al rol PDV durante la migración.';
    END IF;

    UPDATE `sitracalco_authentication_role`
        SET `role_code` = 'PDV'
        WHERE `IdRole` = 6 AND BINARY `nameRole` = BINARY 'PDV' AND `role_code` IS NULL;
    IF ROW_COUNT() <> 1 THEN
        SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'No se pudo identificar de forma única el rol PDV existente.';
    END IF;
    COMMIT;
END$$
DELIMITER ;

CALL migrate_authentication_pdv_assignment();
DROP PROCEDURE migrate_authentication_pdv_assignment;

-- Cargar posteriormente los códigos y nombres corporativos aprobados mediante SQL controlado.
-- Mantener suspendidas las escrituras administrativas hasta desplegar todas las instancias de la API nueva.
