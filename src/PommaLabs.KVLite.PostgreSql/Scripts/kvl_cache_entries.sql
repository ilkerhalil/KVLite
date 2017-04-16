﻿DROP TABLE IF EXISTS `kvl_cache_entries`;

CREATE TABLE IF NOT EXISTS `kvl_cache_entries` (
	`kvle_id` BIGINT(20) UNSIGNED NOT NULL AUTO_INCREMENT COMMENT 'Automatically generated ID.',
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
	UNIQUE `uk_kvle` (`kvle_partition`, `kvle_key`),
	INDEX `ix_kvle_exp_part` (`kvle_expiry` DESC, `kvle_partition` ASC),
	INDEX `fk_kvle_parent0` (`kvle_partition`, `kvle_parent_key0`),
	INDEX `fk_kvle_parent1` (`kvle_partition`, `kvle_parent_key1`),
	INDEX `fk_kvle_parent2` (`kvle_partition`, `kvle_parent_key2`),
	INDEX `fk_kvle_parent3` (`kvle_partition`, `kvle_parent_key3`),
	INDEX `fk_kvle_parent4` (`kvle_partition`, `kvle_parent_key4`),
	CONSTRAINT `fk_kvle_parent0` FOREIGN KEY (`kvle_partition`, `kvle_parent_key0`) REFERENCES `kvl_cache_entries` (`kvle_partition`, `kvle_key`) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT `fk_kvle_parent1` FOREIGN KEY (`kvle_partition`, `kvle_parent_key1`) REFERENCES `kvl_cache_entries` (`kvle_partition`, `kvle_key`) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT `fk_kvle_parent2` FOREIGN KEY (`kvle_partition`, `kvle_parent_key2`) REFERENCES `kvl_cache_entries` (`kvle_partition`, `kvle_key`) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT `fk_kvle_parent3` FOREIGN KEY (`kvle_partition`, `kvle_parent_key3`) REFERENCES `kvl_cache_entries` (`kvle_partition`, `kvle_key`) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT `fk_kvle_parent4` FOREIGN KEY (`kvle_partition`, `kvle_parent_key4`) REFERENCES `kvl_cache_entries` (`kvle_partition`, `kvle_key`) ON UPDATE CASCADE ON DELETE CASCADE
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB
ROW_FORMAT=DYNAMIC
;