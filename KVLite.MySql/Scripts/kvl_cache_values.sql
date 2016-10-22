DROP TABLE IF EXISTS `kvl_cache_values`;

CREATE TABLE IF NOT EXISTS `kvl_cache_values` (
  `kvlv_id` bigint(20) unsigned NOT NULL AUTO_INCREMENT,
  `kvli_hash` bigint(20) NOT NULL,
  `kvlv_value` mediumblob NOT NULL,
  `kvlv_compressed` boolean NOT NULL,
  PRIMARY KEY (`kvlv_id`),
  UNIQUE `uk_kvlv` (`kvli_hash`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;

ALTER TABLE `kvl_cache_values`
  ADD CONSTRAINT `fk_kvlv_hash` FOREIGN KEY (`kvli_hash`) 
  REFERENCES `kvl_cache_items` (`kvli_hash`) ON UPDATE CASCADE ON DELETE CASCADE;
