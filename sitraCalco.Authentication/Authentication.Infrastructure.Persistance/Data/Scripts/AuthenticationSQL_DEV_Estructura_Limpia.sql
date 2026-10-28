-- ============================================================================
-- Authentication - instalacion inicial DEV - MySQL 8 / InnoDB
-- Estructura final del repositorio al 2026-10-26, incluida la asignacion PDV.
-- ============================================================================
-- Seleccionar previamente en Workbench el esquema compartido de SITRA destinado
-- a DEV. Las ocho tablas de Authentication NO deben existir en ese esquema.
-- Este archivo no crea ni altera la base de datos ni las tablas de Inventory.
-- No es una migracion ni un script de reinstalacion: ejecutar una sola vez.
-- Detener la ejecucion ante cualquier error; el DDL de MySQL no es reversible
-- mediante ROLLBACK. No ejecutar los scripts incrementales sobre esta instalacion.
--
-- Fuentes: entidades, SitraCalcoContext, configuraciones EF Core y scripts de
-- Authentication. Tipos, nulabilidad, defaults, indices y FK contrastados con
-- GenerateCreateScript() de EF Core 9.0.17 / Pomelo 9.0.0, sin conexion a MySQL.
-- NO se verifico SHOW CREATE TABLE contra una base real: la conexion disponible
-- no fue confirmada como DEV y no se utilizo.
--
-- Charset: utf8mb4, como el proveedor EF y el script vigente del catalogo PDV.
-- El modelo no fija collation general: se conserva la predeterminada de utf8mb4
-- del servidor MySQL 8. Queda pendiente contrastarla con el esquema fisico.
-- role_code, id_session y refresh_token_hash usan expresamente ascii/ascii_bin.
-- Las FK obligatorias usuario/rol/menu usan ON DELETE CASCADE, segun el modelo
-- efectivo de EF; la jerarquia de menu usa SET NULL y la FK de PDV usa RESTRICT.
-- ON UPDATE RESTRICT explicita el comportamiento predeterminado de InnoDB.
-- Ajuste de compatibilidad MySQL 8: log.created_at usa CURRENT_TIMESTAMP(6),
-- igualando la precision de DATETIME(6). EF declara CURRENT_TIMESTAMP sin
-- precision explicita; se mantiene la inicializacion automatica, sin ON UPDATE.
--
-- La regla PDV => punto de venta obligatorio se valida en el backend, dentro de
-- la transaccion. Para otros roles el backend normaliza la asignacion a NULL.
-- No se agregan triggers ni restricciones UNIQUE sobre IdUser o el PDV individual.
-- Los IDs del catalogo son internos, autoincrementales e independientes de Inventory.
--
-- Dependencia externa de aplicacion: WordPressDatabase / wp_users. No se crea esa
-- tabla ni se agrega una FK externa a wordpressUserId. No se incluyen usuarios,
-- credenciales, sesiones existentes ni datos operativos.

-- ============================================================================
-- 1. ESTRUCTURA FINAL: tablas, indices y relaciones en orden de dependencia.
-- ============================================================================

CREATE TABLE `sitracalco_authentication_log` (
    `log_id` BIGINT NOT NULL AUTO_INCREMENT,
    `action` VARCHAR(50) NOT NULL,
    `module` VARCHAR(100) NOT NULL,
    `description` VARCHAR(500) NOT NULL,
    `user_name` VARCHAR(100) NOT NULL,
    `created_at` DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    PRIMARY KEY (`log_id`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE `sitracalco_authentication_menuoption` (
    `IdMenuOption` INT NOT NULL AUTO_INCREMENT,
    `nameMenuOption` VARCHAR(100) NOT NULL,
    `pathMenuOption` VARCHAR(200) NULL DEFAULT NULL,
    `parentMenuOption` INT NULL DEFAULT NULL,
    `orderMenuOption` INT NOT NULL,
    `statusMenuOption` TINYINT NOT NULL DEFAULT '1',
    PRIMARY KEY (`IdMenuOption`),
    KEY `fk_menu_option_parent` (`parentMenuOption`),
    CONSTRAINT `fk_menu_option_parent`
        FOREIGN KEY (`parentMenuOption`)
        REFERENCES `sitracalco_authentication_menuoption` (`IdMenuOption`)
        ON DELETE SET NULL ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE `sitracalco_authentication_role` (
    `IdRole` INT NOT NULL AUTO_INCREMENT,
    `nameRole` VARCHAR(100) NOT NULL,
    `role_code` VARCHAR(32) CHARACTER SET ascii COLLATE ascii_bin NULL DEFAULT NULL,
    `statusRole` TINYINT NOT NULL DEFAULT '1',
    PRIMARY KEY (`IdRole`),
    UNIQUE KEY `uq_sitracalco_authentication_name` (`nameRole`),
    UNIQUE KEY `uq_auth_role_code` (`role_code`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE `sitracalco_authentication_solution_center` (
    `solution_center_id` INT NOT NULL AUTO_INCREMENT,
    `solution_center_code` VARCHAR(64) NOT NULL,
    `solution_center_name` VARCHAR(200) NOT NULL,
    PRIMARY KEY (`solution_center_id`),
    UNIQUE KEY `uq_auth_solution_center_code` (`solution_center_code`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE `sitracalco_authentication_users` (
    `IdUser` INT NOT NULL AUTO_INCREMENT,
    `wordpressUserId` BIGINT UNSIGNED NOT NULL,
    `userLogin` VARCHAR(60) NOT NULL,
    `userName` VARCHAR(250) NOT NULL,
    `statusUser` TINYINT(1) NOT NULL DEFAULT '1',
    PRIMARY KEY (`IdUser`),
    UNIQUE KEY `uq_sitracalco_authentication_users_user_login` (`userLogin`),
    UNIQUE KEY `uq_sitracalco_authentication_users_user_wordpress_id` (`wordpressUserId`)
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE `sitracalco_authentication_rolemenuoption` (
    `IdRoleMenuOption` INT NOT NULL AUTO_INCREMENT,
    `IdRole` INT NOT NULL,
    `IdMenuOption` INT NOT NULL,
    PRIMARY KEY (`IdRoleMenuOption`),
    UNIQUE KEY `uq_sitracalco_authentication_role_menu_option` (`IdRole`, `IdMenuOption`),
    KEY `fk_sitracalco_authentication_role_menu_option_menu_option` (`IdMenuOption`),
    CONSTRAINT `fk_sitracalco_authentication_role_menu_option_menu_option`
        FOREIGN KEY (`IdMenuOption`)
        REFERENCES `sitracalco_authentication_menuoption` (`IdMenuOption`)
        ON DELETE CASCADE ON UPDATE RESTRICT,
    CONSTRAINT `fk_sitracalco_authentication_role_menu_option_role`
        FOREIGN KEY (`IdRole`)
        REFERENCES `sitracalco_authentication_role` (`IdRole`)
        ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE `sitracalco_authentication_sessions` (
    `id_session` VARCHAR(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `id_user` INT NOT NULL,
    `created_at` DATETIME(6) NOT NULL,
    `last_activity_at` DATETIME(6) NOT NULL,
    `expires_at` DATETIME(6) NOT NULL,
    `revoked_at` DATETIME(6) NULL DEFAULT NULL,
    `refresh_token_hash` VARCHAR(64) CHARACTER SET ascii COLLATE ascii_bin NULL DEFAULT NULL,
    PRIMARY KEY (`id_session`),
    KEY `ix_authentication_sessions_user` (`id_user`),
    CONSTRAINT `fk_authentication_sessions_user`
        FOREIGN KEY (`id_user`)
        REFERENCES `sitracalco_authentication_users` (`IdUser`)
        ON DELETE CASCADE ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

CREATE TABLE `sitracalco_authentication_userrole` (
    `IdUserRole` INT NOT NULL AUTO_INCREMENT,
    `IdUser` INT NOT NULL,
    `IdRole` INT NOT NULL,
    `solution_center_id` INT NULL DEFAULT NULL,
    PRIMARY KEY (`IdUserRole`),
    UNIQUE KEY `uq_sitracalco_authentication_user_role` (`IdUser`, `IdRole`),
    KEY `fk_sitracalco_authentication_user_role_role` (`IdRole`),
    KEY `ix_auth_userrole_solution_center` (`solution_center_id`),
    CONSTRAINT `fk_sitracalco_authentication_user_role_user`
        FOREIGN KEY (`IdUser`)
        REFERENCES `sitracalco_authentication_users` (`IdUser`)
        ON DELETE CASCADE ON UPDATE RESTRICT,
    CONSTRAINT `fk_sitracalco_authentication_user_role_role`
        FOREIGN KEY (`IdRole`)
        REFERENCES `sitracalco_authentication_role` (`IdRole`)
        ON DELETE CASCADE ON UPDATE RESTRICT,
    CONSTRAINT `fk_auth_userrole_solution_center`
        FOREIGN KEY (`solution_center_id`)
        REFERENCES `sitracalco_authentication_solution_center` (`solution_center_id`)
        ON DELETE RESTRICT ON UPDATE RESTRICT
) ENGINE=InnoDB DEFAULT CHARACTER SET=utf8mb4;

-- ============================================================================
-- 2. DATOS INICIALES VERIFICADOS, separados de la estructura.
-- ============================================================================
-- PDV: nombre y estado confirmados durante el desarrollo; clave funcional
-- documentada en el modelo y en AddPointOfSaleAssignments.sql.
-- No se fija IdRole=6: ese numero pertenecia a la instalacion existente.
-- En una base nueva el ID se genera y el backend reconoce role_code='PDV'.
-- La condicion evita duplicados si se ejecuta solamente esta seccion otra vez;
-- no transforma ni actualiza un rol preexistente con el mismo nombre o codigo.

INSERT INTO `sitracalco_authentication_role` (`nameRole`, `role_code`, `statusRole`)
SELECT 'PDV', 'PDV', 1
FROM `sitracalco_authentication_role`
WHERE `nameRole` = 'PDV' OR `role_code` = 'PDV'
HAVING COUNT(*) = 0;

-- PENDIENTE: listado oficial de los otros roles, opciones de menu y sus
-- asociaciones. No hay semillas verificadas de esos datos en Authentication.
-- No se conceden permisos ni se vinculan opciones de menu al rol PDV aqui.
--
-- PENDIENTE: listado corporativo nominativo de los 35 puntos de venta.
-- No esta disponible en los archivos revisados ni en el requerimiento recibido.
-- No generar registros a partir de los rangos R01-R25, H01-H09 y C01-C02:
-- los rangos completos sumarian 36, por lo que no permiten deducir el listado.
-- La futura carga aprobada debe omitir solution_center_id y suministrar el
-- codigo y nombre oficiales. No se incluyen PV-EVG ni datos de las pruebas.
--
-- Para autenticar en DEV faltan configuracion externa (conexiones, JWT/SSO),
-- acceso a WordPress y el alta autorizada de usuarios con roles y opciones de
-- menu activas. Un rol sin opciones de menu no habilita el acceso al aplicativo.
-- Para asignar usuarios PDV tambien debe cargarse primero el catalogo oficial.

-- ============================================================================
-- 3. VERIFICACIONES FINALES DE SOLO LECTURA.
-- ============================================================================
-- Resultado esperado: ocho tablas InnoDB. Permite comprobar la collation efectiva.
SELECT `TABLE_NAME`, `ENGINE`, `TABLE_COLLATION`
FROM `INFORMATION_SCHEMA`.`TABLES`
WHERE `TABLE_SCHEMA` = DATABASE()
  AND `TABLE_TYPE` = 'BASE TABLE'
  AND `TABLE_NAME` IN (
      'sitracalco_authentication_log',
      'sitracalco_authentication_menuoption',
      'sitracalco_authentication_role',
      'sitracalco_authentication_solution_center',
      'sitracalco_authentication_users',
      'sitracalco_authentication_rolemenuoption',
      'sitracalco_authentication_sessions',
      'sitracalco_authentication_userrole'
  )
ORDER BY `TABLE_NAME`;

-- Resultado esperado: siete FK; la de solution_center usa RESTRICT / RESTRICT.
SELECT `TABLE_NAME`, `CONSTRAINT_NAME`, `REFERENCED_TABLE_NAME`,
       `DELETE_RULE`, `UPDATE_RULE`
FROM `INFORMATION_SCHEMA`.`REFERENTIAL_CONSTRAINTS`
WHERE `CONSTRAINT_SCHEMA` = DATABASE()
  AND `TABLE_NAME` IN (
      'sitracalco_authentication_menuoption',
      'sitracalco_authentication_rolemenuoption',
      'sitracalco_authentication_sessions',
      'sitracalco_authentication_userrole'
  )
ORDER BY `TABLE_NAME`, `CONSTRAINT_NAME`;

-- Resultado esperado tras este archivo: un rol funcional PDV y cero puntos de venta.
SELECT
    (SELECT COUNT(*) FROM `sitracalco_authentication_role`
     WHERE `role_code` = 'PDV' AND `nameRole` = 'PDV' AND `statusRole` = 1) AS `roles_pdv`,
    (SELECT COUNT(*) FROM `sitracalco_authentication_solution_center`) AS `puntos_de_venta`;
