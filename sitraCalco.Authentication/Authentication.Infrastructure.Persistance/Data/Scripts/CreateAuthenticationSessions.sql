CREATE TABLE `sitracalco_authentication_sessions` (
    `id_session` varchar(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `id_user` int NOT NULL,
    `created_at` datetime(6) NOT NULL,
    `last_activity_at` datetime(6) NOT NULL,
    `expires_at` datetime(6) NOT NULL,
    `revoked_at` datetime(6) NULL DEFAULT NULL,
    `refresh_token_hash` varchar(64) CHARACTER SET ascii COLLATE ascii_bin NULL DEFAULT NULL,
    PRIMARY KEY (`id_session`),
    KEY `ix_authentication_sessions_user` (`id_user`),
    CONSTRAINT `fk_authentication_sessions_user` FOREIGN KEY (`id_user`)
        REFERENCES `sitracalco_authentication_users` (`IdUser`) ON DELETE CASCADE
) ENGINE=InnoDB;
