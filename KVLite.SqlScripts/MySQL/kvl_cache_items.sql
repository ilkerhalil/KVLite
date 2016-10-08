CREATE TABLE IF NOT EXISTS `kvl_cache_items` (
  `kvli_partition` varchar(255) NOT NULL,
  `kvli_key` varchar(255) NOT NULL,
  `kvli_value` mediumblob NOT NULL,
  `kvli_creation` bigint(20) unsigned NOT NULL,
  `kvli_expiry` bigint(20) unsigned NOT NULL,
  `kvli_interval` bigint(20) unsigned NOT NULL,
  `kvli_parent0` varchar(255) DEFAULT NULL,
  `kvli_parent1` varchar(255) DEFAULT NULL,
  `kvli_parent2` varchar(255) DEFAULT NULL,
  `kvli_parent3` varchar(255) DEFAULT NULL,
  `kvli_parent4` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`kvli_partition`, `kvli_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

ALTER TABLE `kvl_cache_items`
	ADD CONSTRAINT `fk_kvl_cache_items_parent0` FOREIGN KEY (`kvli_partition`, `kvli_parent0`) 
    REFERENCES `kvl_cache_items` (`kvli_partition`, `kvli_key`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_items`
	ADD CONSTRAINT `fk_kvl_cache_items_parent1` FOREIGN KEY (`kvli_partition`, `kvli_parent1`) 
    REFERENCES `kvl_cache_items` (`kvli_partition`, `kvli_key`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_items`
	ADD CONSTRAINT `fk_kvl_cache_items_parent2` FOREIGN KEY (`kvli_partition`, `kvli_parent2`) 
    REFERENCES `kvl_cache_items` (`kvli_partition`, `kvli_key`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_items`
	ADD CONSTRAINT `fk_kvl_cache_items_parent3` FOREIGN KEY (`kvli_partition`, `kvli_parent3`) 
    REFERENCES `kvl_cache_items` (`kvli_partition`, `kvli_key`) ON UPDATE CASCADE ON DELETE CASCADE;

ALTER TABLE `kvl_cache_items`
	ADD CONSTRAINT `fk_kvl_cache_items_parent4` FOREIGN KEY (`kvli_partition`, `kvli_parent4`) 
    REFERENCES `kvl_cache_items` (`kvli_partition`, `kvli_key`) ON UPDATE CASCADE ON DELETE CASCADE;