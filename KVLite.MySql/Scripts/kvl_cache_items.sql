DROP TABLE IF EXISTS `kvl_cache_items`;

CREATE TABLE IF NOT EXISTS `kvl_cache_items` (
  `kvli_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `kvli_hash` bigint(20) NOT NULL,
  `kvli_partition` varchar(255) NOT NULL,
  `kvli_key` varchar(255) NOT NULL,
  `kvli_creation` bigint(20) unsigned NOT NULL,
  `kvli_expiry` bigint(20) unsigned NOT NULL,
  `kvli_interval` bigint(20) unsigned NOT NULL,
  `kvli_compressed` boolean NOT NULL,
  `kvli_value` mediumblob NOT NULL,
  `kvli_parent_hash0` bigint(20) DEFAULT NULL,
  `kvli_parent_key0` varchar(255) DEFAULT NULL,
  `kvli_parent_hash1` bigint(20) DEFAULT NULL,
  `kvli_parent_key1` varchar(255) DEFAULT NULL,
  `kvli_parent_hash2` bigint(20) DEFAULT NULL,
  `kvli_parent_key2` varchar(255) DEFAULT NULL,
  `kvli_parent_hash3` bigint(20) DEFAULT NULL,
  `kvli_parent_key3` varchar(255) DEFAULT NULL,
  `kvli_parent_hash4` bigint(20) DEFAULT NULL,
  `kvli_parent_key4` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`kvli_id`),
  UNIQUE `uk_kvli` (`kvli_hash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

ALTER TABLE `kvl_cache_items`
  ADD INDEX `ix_kvli_exp_part` (`kvli_expiry`, `kvli_partition`);

ALTER TABLE `kvl_cache_items`
  ADD CONSTRAINT `fk_kvli_parent0` FOREIGN KEY (`kvli_parent_hash0`) 
  REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_items`
  ADD CONSTRAINT `fk_kvli_parent1` FOREIGN KEY (`kvli_parent_hash1`) 
  REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_items`
  ADD CONSTRAINT `fk_kvli_parent2` FOREIGN KEY (`kvli_parent_hash2`) 
  REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_items`
  ADD CONSTRAINT `fk_kvli_parent3` FOREIGN KEY (`kvli_parent_hash3`) 
  REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_items`
  ADD CONSTRAINT `fk_kvli_parent4` FOREIGN KEY (`kvli_parent_hash4`) 
  REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE;