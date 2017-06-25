DROP TABLE IF EXISTS `kvl_cache_entries`;

CREATE TABLE IF NOT EXISTS `kvl_cache_entries` (
    `kvle_id` BIGINT(20) UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Automatically generated ID.',
    `kvle_hash` BIGINT(20) NOT NULL COMMENT 'Hash of partition and key.',
    `kvle_partition` VARCHAR(255) NOT NULL COMMENT 'A partition holds a group of related keys.',
    `kvle_key` VARCHAR(255) NOT NULL COMMENT 'A key uniquely identifies an entry inside a partition.',
    `kvle_expiry` BIGINT(20) UNSIGNED NOT NULL COMMENT 'When the entry will expire, expressed as seconds after UNIX epoch.',
    `kvle_interval` BIGINT(20) UNSIGNED NOT NULL COMMENT 'How many seconds should be used to extend expiry time when the entry is retrieved.',
    `kvle_value` MEDIUMBLOB NOT NULL COMMENT 'Serialized and optionally compressed content of this entry.',
    `kvle_compressed` TINYINT(1) NOT NULL COMMENT 'Whether the entry content was compressed or not.',
    `kvle_creation` BIGINT(20) UNSIGNED NOT NULL COMMENT 'When the entry was created, expressed as seconds after UNIX epoch.',
    `kvle_parent_key0` VARCHAR(255) NULL DEFAULT NULL COMMENT 'Optional parent entry, used to link entries in a hierarchical way.',
    `kvle_parent_key1` VARCHAR(255) NULL DEFAULT NULL COMMENT 'Optional parent entry, used to link entries in a hierarchical way.',
    `kvle_parent_key2` VARCHAR(255) NULL DEFAULT NULL COMMENT 'Optional parent entry, used to link entries in a hierarchical way.',
    `kvle_parent_key3` VARCHAR(255) NULL DEFAULT NULL COMMENT 'Optional parent entry, used to link entries in a hierarchical way.',
    `kvle_parent_key4` VARCHAR(255) NULL DEFAULT NULL COMMENT 'Optional parent entry, used to link entries in a hierarchical way.',
    PRIMARY KEY (`kvle_id`),
    UNIQUE `uk_kvle` (`kvle_hash`),
    INDEX `ix_kvle_part_exp` (`kvle_partition` ASC, `kvle_expiry` DESC)
)
COLLATE='utf8_general_ci'
ENGINE=MyISAM
ROW_FORMAT=DYNAMIC
;
