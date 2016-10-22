DROP TABLE IF EXISTS `kvl_cache_items`;

CREATE TABLE IF NOT EXISTS `kvl_cache_items` (
	`kvli_id` BIGINT(20) UNSIGNED NOT NULL AUTO_INCREMENT,
	`kvli_hash` BIGINT(20) NOT NULL,
	`kvli_partition` VARCHAR(255) NOT NULL,
	`kvli_key` VARCHAR(255) NOT NULL,
	`kvli_creation` BIGINT(20) UNSIGNED NOT NULL,
	`kvli_expiry` BIGINT(20) UNSIGNED NOT NULL,
	`kvli_interval` BIGINT(20) UNSIGNED NOT NULL,
	`kvli_parent_hash0` BIGINT(20) NULL DEFAULT NULL,
	`kvli_parent_key0` VARCHAR(255) NULL DEFAULT NULL,
	`kvli_parent_hash1` BIGINT(20) NULL DEFAULT NULL,
	`kvli_parent_key1` VARCHAR(255) NULL DEFAULT NULL,
	`kvli_parent_hash2` BIGINT(20) NULL DEFAULT NULL,
	`kvli_parent_key2` VARCHAR(255) NULL DEFAULT NULL,
	`kvli_parent_hash3` BIGINT(20) NULL DEFAULT NULL,
	`kvli_parent_key3` VARCHAR(255) NULL DEFAULT NULL,
	`kvli_parent_hash4` BIGINT(20) NULL DEFAULT NULL,
	`kvli_parent_key4` VARCHAR(255) NULL DEFAULT NULL,
	PRIMARY KEY (`kvli_id`),
	UNIQUE INDEX `uk_kvli` (`kvli_hash`),
	INDEX `ix_kvli_exp_part` (`kvli_expiry`, `kvli_partition`),
	INDEX `fk_kvli_parent0` (`kvli_parent_hash0`),
	INDEX `fk_kvli_parent1` (`kvli_parent_hash1`),
	INDEX `fk_kvli_parent2` (`kvli_parent_hash2`),
	INDEX `fk_kvli_parent3` (`kvli_parent_hash3`),
	INDEX `fk_kvli_parent4` (`kvli_parent_hash4`),
	CONSTRAINT `fk_kvli_parent0` FOREIGN KEY (`kvli_parent_hash0`) REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT `fk_kvli_parent1` FOREIGN KEY (`kvli_parent_hash1`) REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT `fk_kvli_parent2` FOREIGN KEY (`kvli_parent_hash2`) REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT `fk_kvli_parent3` FOREIGN KEY (`kvli_parent_hash3`) REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE,
	CONSTRAINT `fk_kvli_parent4` FOREIGN KEY (`kvli_parent_hash4`) REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB
;
