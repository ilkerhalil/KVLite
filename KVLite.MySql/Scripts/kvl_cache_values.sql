DROP TABLE IF EXISTS `kvl_cache_values`;

CREATE TABLE IF NOT EXISTS `kvl_cache_values` (
	`kvli_hash` BIGINT(20) NOT NULL,
	`kvlv_value` MEDIUMBLOB NOT NULL,
	`kvlv_compressed` TINYINT(1) NOT NULL,
	PRIMARY KEY (`kvli_hash`),
	CONSTRAINT `fk_kvlv_hash` FOREIGN KEY (`kvli_hash`) REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE
)
COLLATE='utf8_general_ci'
ENGINE=InnoDB
;
