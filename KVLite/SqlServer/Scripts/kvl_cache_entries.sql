DROP TABLE IF EXISTS `kvl_cache_entries`;

CREATE TABLE IF NOT EXISTS `kvl_cache_entries` (
  `kvle_hash` bigint(20) NOT NULL,
  `kvle_partition` varchar(255) NOT NULL,
  `kvle_key` varchar(255) NOT NULL,
  `kvle_creation` bigint(20) unsigned NOT NULL,
  `kvle_expiry` bigint(20) unsigned NOT NULL,
  `kvle_interval` bigint(20) unsigned NOT NULL,
  `kvle_compressed` boolean NOT NULL,
  `kvle_parent_hash0` bigint(20) DEFAULT NULL,
  `kvle_parent_key0` varchar(255) DEFAULT NULL,
  `kvle_parent_hash1` bigint(20) DEFAULT NULL,
  `kvle_parent_key1` varchar(255) DEFAULT NULL,
  `kvle_parent_hash2` bigint(20) DEFAULT NULL,
  `kvle_parent_key2` varchar(255) DEFAULT NULL,
  `kvle_parent_hash3` bigint(20) DEFAULT NULL,
  `kvle_parent_key3` varchar(255) DEFAULT NULL,
  `kvle_parent_hash4` bigint(20) DEFAULT NULL,
  `kvle_parent_key4` varchar(255) DEFAULT NULL,
  `kvle_value` mediumblob NOT NULL,
  PRIMARY KEY (`kvle_hash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

ALTER TABLE `kvl_cache_entries`
  ADD INDEX `ix_kvle_exp_part` (`kvle_expiry`, `kvle_partition`);

ALTER TABLE `kvl_cache_entries`
  ADD CONSTRAINT `fk_kvle_parent0` FOREIGN KEY (`kvle_parent_hash0`) 
  REFERENCES `kvl_cache_entries` (`kvle_hash`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_entries`
  ADD CONSTRAINT `fk_kvle_parent1` FOREIGN KEY (`kvle_parent_hash1`) 
  REFERENCES `kvl_cache_entries` (`kvle_hash`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_entries`
  ADD CONSTRAINT `fk_kvle_parent2` FOREIGN KEY (`kvle_parent_hash2`) 
  REFERENCES `kvl_cache_entries` (`kvle_hash`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_entries`
  ADD CONSTRAINT `fk_kvle_parent3` FOREIGN KEY (`kvle_parent_hash3`) 
  REFERENCES `kvl_cache_entries` (`kvle_hash`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_entries`
  ADD CONSTRAINT `fk_kvle_parent4` FOREIGN KEY (`kvle_parent_hash4`) 
  REFERENCES `kvl_cache_entries` (`kvle_hash`) ON UPDATE CASCADE ON DELETE CASCADE;
