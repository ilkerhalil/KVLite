CREATE TABLE `kvl_cache_items` (
  `kvli_id` bigint(20) NOT NULL,
  `kvli_partition` varchar(255) NOT NULL,
  `kvli_key` varchar(255) NOT NULL,
  `kvli_value` mediumblob NOT NULL,
  `kvli_creation` bigint(20) unsigned NOT NULL,
  `kvli_expiry` bigint(20) unsigned NOT NULL,
  `kvli_interval` bigint(20) unsigned NOT NULL,
  PRIMARY KEY (`kvli_id`),
  UNIQUE KEY `uk_kvl_cache_items` (`kvli_partition`,`kvli_key`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
