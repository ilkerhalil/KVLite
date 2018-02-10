/* Optional setup */

CREATE DATABASE `kvlite`;

USE `kvlite`;

DROP TABLE IF EXISTS `kvl_cache_entries`;

/* Table & Comments */

CREATE TABLE IF NOT EXISTS `kvl_cache_entries` (
    `kvle_id` BIGINT(20) UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Automatically generated ID.',
    `kvle_hash` BIGINT(20) SIGNED NOT NULL COMMENT 'Hash of partition and key.',
    `kvle_expiry` BIGINT(20) UNSIGNED NOT NULL COMMENT 'When the entry will expire, expressed as seconds after UNIX epoch.',
    `kvle_interval` BIGINT(20) UNSIGNED NOT NULL COMMENT 'How many seconds should be used to extend expiry time when the entry is retrieved.',
    `kvle_value` MEDIUMBLOB NOT NULL COMMENT 'Serialized and optionally compressed content of this entry.',
    `kvle_compressed` TINYINT(1) UNSIGNED NOT NULL COMMENT 'Whether the entry content was compressed or not.',
    `kvle_partition` VARCHAR(2000) NOT NULL COMMENT 'A partition holds a group of related keys.',
    `kvle_key` VARCHAR(2000) NOT NULL COMMENT 'A key uniquely identifies an entry inside a partition.',
    `kvle_creation` BIGINT(20) UNSIGNED NOT NULL COMMENT 'When the entry was created, expressed as seconds after UNIX epoch.',
    `kvle_parent_hash0` BIGINT(20) SIGNED DEFAULT NULL COMMENT 'Optional parent entry hash, used to link entries in a hierarchical way.',
    `kvle_parent_key0` VARCHAR(2000) NULL DEFAULT NULL COMMENT 'Optional parent entry key, used to link entries in a hierarchical way.',
    `kvle_parent_hash1` BIGINT(20) SIGNED DEFAULT NULL COMMENT 'Optional parent entry hash, used to link entries in a hierarchical way.',
    `kvle_parent_key1` VARCHAR(2000) NULL DEFAULT NULL COMMENT 'Optional parent entry key, used to link entries in a hierarchical way.',
    `kvle_parent_hash2` BIGINT(20) SIGNED DEFAULT NULL COMMENT 'Optional parent entry hash, used to link entries in a hierarchical way.',
    `kvle_parent_key2` VARCHAR(2000) NULL DEFAULT NULL COMMENT 'Optional parent entry key, used to link entries in a hierarchical way.',
    PRIMARY KEY (`kvle_id`),
    UNIQUE `uk_kvl_cache_entries` (`kvle_hash`)
)
COLLATE='utf8_general_ci'
ENGINE=MyISAM /* On MariaDB, you can replace this with Aria */
ROW_FORMAT=DYNAMIC
;
