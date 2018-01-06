CREATE SCHEMA IF NOT EXISTS kvlite;

DROP TABLE IF EXISTS kvlite.kvl_cache_entries;

CREATE UNLOGGED TABLE kvlite.kvl_cache_entries
(
    kvle_id bigserial NOT NULL,
    kvle_hash bigint NOT NULL,
    kvle_expiry bigint NOT NULL,
    kvle_interval bigint NOT NULL,
    kvle_value bytea NOT NULL,
    kvle_compressed smallint NOT NULL,
    kvle_partition character varying(2000) NOT NULL,
    kvle_key character varying(2000) NOT NULL,
    kvle_creation bigint NOT NULL,
    kvle_parent_hash0 bigint,
    kvle_parent_key0 character varying(2000),
    kvle_parent_hash1 bigint,
    kvle_parent_key1 character varying(2000),
    kvle_parent_hash2 bigint,
    kvle_parent_key2 character varying(2000),
    CONSTRAINT pk_kvle PRIMARY KEY (kvle_id),
    CONSTRAINT uk_kvle UNIQUE (kvle_hash)
)
WITH (
    OIDS = FALSE
);

CREATE INDEX ix_kvle_parent0
    ON kvlite.kvl_cache_entries(kvle_parent_hash0)
 WHERE kvle_parent_hash0 IS NOT NULL;

CREATE INDEX ix_kvle_parent1
    ON kvlite.kvl_cache_entries(kvle_parent_hash1)
 WHERE kvle_parent_hash1 IS NOT NULL;

CREATE INDEX ix_kvle_parent2
    ON kvlite.kvl_cache_entries(kvle_parent_hash2)
 WHERE kvle_parent_hash2 IS NOT NULL;

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_id
    IS 'Automatically generated ID.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_hash
    IS 'Hash of partition and key.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_expiry
    IS 'When the entry will expire, expressed as seconds after UNIX epoch.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_interval
    IS 'How many seconds should be used to extend expiry time when the entry is retrieved.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_value
    IS 'Serialized and optionally compressed content of this entry.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_compressed
    IS 'Whether the entry content was compressed or not.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_partition
    IS 'A partition holds a group of related keys.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_key
    IS 'A key uniquely identifies an entry inside a partition.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_creation
    IS 'When the entry was created, expressed as seconds after UNIX epoch.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_parent_hash0
    IS 'Optional parent entry hash, used to link entries in a hierarchical way.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_parent_key0
    IS 'Optional parent entry key, used to link entries in a hierarchical way.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_parent_hash1
    IS 'Optional parent entry hash, used to link entries in a hierarchical way.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_parent_key1
    IS 'Optional parent entry key, used to link entries in a hierarchical way.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_parent_hash2
    IS 'Optional parent entry hash, used to link entries in a hierarchical way.';

COMMENT ON COLUMN kvlite.kvl_cache_entries.kvle_parent_key2
    IS 'Optional parent entry key, used to link entries in a hierarchical way.';