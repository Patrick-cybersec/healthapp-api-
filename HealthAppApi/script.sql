Build started...
Build succeeded.
CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20250520113026_InitialCreate') THEN

    ALTER DATABASE CHARACTER SET utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20250520113026_InitialCreate') THEN

    CREATE TABLE `activity_records` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `UserId` longtext CHARACTER SET utf8mb4 NOT NULL,
        `ActivityType` longtext CHARACTER SET utf8mb4 NOT NULL,
        `HeartRate` float NOT NULL,
        `Mood` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Duration` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Exercises` longtext CHARACTER SET utf8mb4 NOT NULL,
        `Created_At` datetime(6) NOT NULL,
        CONSTRAINT `PK_activity_records` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20250520113026_InitialCreate') THEN

    CREATE TABLE `billboard_records` (
        `Id` int NOT NULL AUTO_INCREMENT,
        `song_title` longtext CHARACTER SET utf8mb4 NOT NULL,
        `artist` longtext CHARACTER SET utf8mb4 NOT NULL,
        `chart_rank` int NOT NULL,
        `star_number` int NOT NULL,
        `updated_at` datetime(6) NOT NULL,
        CONSTRAINT `PK_billboard_records` PRIMARY KEY (`Id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20250520113026_InitialCreate') THEN

    CREATE TABLE `users` (
        `id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
        `name` longtext CHARACTER SET utf8mb4 NOT NULL,
        `age` int NOT NULL,
        `sex` longtext CHARACTER SET utf8mb4 NULL,
        `created_at` datetime(6) NOT NULL,
        `isAdmin` tinyint(1) NOT NULL,
        `password` longtext CHARACTER SET utf8mb4 NOT NULL,
        `FcmToken` longtext CHARACTER SET utf8mb4 NOT NULL,
        CONSTRAINT `PK_users` PRIMARY KEY (`id`)
    ) CHARACTER SET=utf8mb4;

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

DROP PROCEDURE IF EXISTS MigrationsScript;
DELIMITER //
CREATE PROCEDURE MigrationsScript()
BEGIN
    IF NOT EXISTS(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = '20250520113026_InitialCreate') THEN

    INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
    VALUES ('20250520113026_InitialCreate', '8.0.8');

    END IF;
END //
DELIMITER ;
CALL MigrationsScript();
DROP PROCEDURE MigrationsScript;

COMMIT;


