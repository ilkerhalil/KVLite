﻿DROP TABLE "KVLITE"."KVL_CACHE_ENTRIES";

CREATE TABLE "KVLITE"."KVL_CACHE_ENTRIES" 
(	 
    "KVLE_PARTITION" NVARCHAR2(255) NOT NULL ENABLE, 
    "KVLE_KEY" NVARCHAR2(255) NOT NULL ENABLE,  
    "KVLE_EXPIRY" NUMBER(20,0) NOT NULL ENABLE, 
    "KVLE_INTERVAL" NUMBER(20,0) NOT NULL ENABLE,
    "KVLE_VALUE" BLOB NOT NULL ENABLE, 
    "KVLE_COMPRESSED" NUMBER(1,0) NOT NULL ENABLE,
    "KVLE_CREATION" NUMBER(20,0) NOT NULL ENABLE, 
    "KVLE_PARENT_KEY0" NVARCHAR2(255), 
    "KVLE_PARENT_KEY1" NVARCHAR2(255), 
    "KVLE_PARENT_KEY2" NVARCHAR2(255), 
    "KVLE_PARENT_KEY3" NVARCHAR2(255), 
    "KVLE_PARENT_KEY4" NVARCHAR2(255), 
    CONSTRAINT "PK_KVLE" PRIMARY KEY ("KVLE_PARTITION", "KVLE_KEY") ENABLE,
    CONSTRAINT "FK_KVLE_PARENT0" FOREIGN KEY ("KVLE_PARTITION", "KVLE_PARENT_KEY0")
    REFERENCES "KVLITE"."KVL_CACHE_ENTRIES" ("KVLE_PARTITION", "KVLE_KEY") ON DELETE CASCADE ENABLE,
    CONSTRAINT "FK_KVLE_PARENT1" FOREIGN KEY ("KVLE_PARTITION", "KVLE_PARENT_KEY1")
    REFERENCES "KVLITE"."KVL_CACHE_ENTRIES" ("KVLE_PARTITION", "KVLE_KEY") ON DELETE CASCADE ENABLE,
    CONSTRAINT "FK_KVLE_PARENT2" FOREIGN KEY ("KVLE_PARTITION", "KVLE_PARENT_KEY2")
    REFERENCES "KVLITE"."KVL_CACHE_ENTRIES" ("KVLE_PARTITION", "KVLE_KEY") ON DELETE CASCADE ENABLE,
    CONSTRAINT "FK_KVLE_PARENT3" FOREIGN KEY ("KVLE_PARTITION", "KVLE_PARENT_KEY3")
    REFERENCES "KVLITE"."KVL_CACHE_ENTRIES" ("KVLE_PARTITION", "KVLE_KEY") ON DELETE CASCADE ENABLE,
    CONSTRAINT "FK_KVLE_PARENT4" FOREIGN KEY ("KVLE_PARTITION", "KVLE_PARENT_KEY4")
    REFERENCES "KVLITE"."KVL_CACHE_ENTRIES" ("KVLE_PARTITION", "KVLE_KEY") ON DELETE CASCADE ENABLE
) 
LOB ("KVLE_VALUE") STORE AS (CACHE READS ENABLE STORAGE IN ROW);

CREATE INDEX "KVLITE"."IX_KVLE_EXP_PART" ON "KVLITE"."KVL_CACHE_ENTRIES" ("KVLE_EXPIRY" DESC, "KVLE_PARTITION" ASC);

CREATE INDEX "KVLITE"."FK_KVLE_PARENT0" ON "KVLITE"."KVL_CACHE_ENTRIES" ("KVLE_PARTITION", "KVLE_PARENT_KEY0");
CREATE INDEX "KVLITE"."FK_KVLE_PARENT1" ON "KVLITE"."KVL_CACHE_ENTRIES" ("KVLE_PARTITION", "KVLE_PARENT_KEY1");
CREATE INDEX "KVLITE"."FK_KVLE_PARENT2" ON "KVLITE"."KVL_CACHE_ENTRIES" ("KVLE_PARTITION", "KVLE_PARENT_KEY2");
CREATE INDEX "KVLITE"."FK_KVLE_PARENT3" ON "KVLITE"."KVL_CACHE_ENTRIES" ("KVLE_PARTITION", "KVLE_PARENT_KEY3");
CREATE INDEX "KVLITE"."FK_KVLE_PARENT4" ON "KVLITE"."KVL_CACHE_ENTRIES" ("KVLE_PARTITION", "KVLE_PARENT_KEY4");

COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARTITION" IS 'A partition holds a group of related keys.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_KEY" IS 'A key uniquely identifies an entry inside a partition.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_EXPIRY" IS 'When the entry will expire, expressed as seconds after UNIX epoch.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_INTERVAL" IS 'How many seconds should be used to extend expiry time when the entry is retrieved.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_VALUE" IS 'Serialized and optionally compressed content of this entry.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_COMPRESSED" IS 'Whether the entry content was compressed or not.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_CREATION" IS 'When the entry was created, expressed as seconds after UNIX epoch.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARENT_KEY0" IS 'Optional parent entry, used to link entries in a hierarchical way.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARENT_KEY1" IS 'Optional parent entry, used to link entries in a hierarchical way.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARENT_KEY2" IS 'Optional parent entry, used to link entries in a hierarchical way.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARENT_KEY3" IS 'Optional parent entry, used to link entries in a hierarchical way.';
COMMENT ON COLUMN "KVLITE"."KVL_CACHE_ENTRIES"."KVLE_PARENT_KEY4" IS 'Optional parent entry, used to link entries in a hierarchical way.';