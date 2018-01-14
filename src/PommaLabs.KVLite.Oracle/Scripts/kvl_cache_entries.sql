﻿/* Optional setup */

DROP TABLE "KVLITE"."KVL_CACHE_ENTRIES";

/* Table */

CREATE TABLE "KVLITE"."KVL_CACHE_ENTRIES"
(
    "KVLE_ID" RAW(16) DEFAULT SYS_GUID() NOT NULL ENABLE,
    "KVLE_HASH" NUMBER(20,0) NOT NULL ENABLE,
    "KVLE_EXPIRY" NUMBER(20,0) NOT NULL ENABLE,
    "KVLE_INTERVAL" NUMBER(20,0) NOT NULL ENABLE,
    "KVLE_VALUE" BLOB NOT NULL ENABLE,
    "KVLE_COMPRESSED" NUMBER(1,0) NOT NULL ENABLE,
    "KVLE_PARTITION" NVARCHAR2(2000) NOT NULL ENABLE,
    "KVLE_KEY" NVARCHAR2(2000) NOT NULL ENABLE,
    "KVLE_CREATION" NUMBER(20,0) NOT NULL ENABLE,
    "KVLE_PARENT_HASH0" NUMBER(20,0),
    "KVLE_PARENT_KEY0" NVARCHAR2(2000),
    "KVLE_PARENT_HASH1" NUMBER(20,0),
    "KVLE_PARENT_KEY1" NVARCHAR2(2000),
    "KVLE_PARENT_HASH2" NUMBER(20,0),
    "KVLE_PARENT_KEY2" NVARCHAR2(2000),
    CONSTRAINT "PK_KVL_CACHE_ENTRIES" PRIMARY KEY ("KVLE_ID") ENABLE,
    CONSTRAINT "UK_KVL_CACHE_ENTRIES" UNIQUE ("KVLE_HASH") ENABLE
)
LOB ("KVLE_VALUE") STORE AS (CACHE READS ENABLE STORAGE IN ROW);

/* Comments */

COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_ID" IS 'Automatically generated ID.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_HASH" IS 'Hash of partition and key.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_EXPIRY" IS 'When the entry will expire, expressed as seconds after UNIX epoch.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_INTERVAL" IS 'How many seconds should be used to extend expiry time when the entry is retrieved.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_VALUE" IS 'Serialized and optionally compressed content of this entry.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_COMPRESSED" IS 'Whether the entry content was compressed or not.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARTITION" IS 'A partition holds a group of related keys.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_KEY" IS 'A key uniquely identifies an entry inside a partition.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_CREATION" IS 'When the entry was created, expressed as seconds after UNIX epoch.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARENT_HASH0" IS 'Optional parent entry hash, used to link entries in a hierarchical way.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARENT_KEY0" IS 'Optional parent entry key, used to link entries in a hierarchical way.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARENT_HASH1" IS 'Optional parent entry hash, used to link entries in a hierarchical way.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARENT_KEY1" IS 'Optional parent entry key, used to link entries in a hierarchical way.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARENT_HASH2" IS 'Optional parent entry hash, used to link entries in a hierarchical way.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARENT_KEY2" IS 'Optional parent entry key, used to link entries in a hierarchical way.';
