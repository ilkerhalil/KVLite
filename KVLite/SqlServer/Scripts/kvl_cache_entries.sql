CREATE TABLE [dbo].[kvl_cache_entries] (
    [kvle_id]          UNIQUEIDENTIFIER NOT NULL,
    [kvle_partition]   NVARCHAR (255)   NOT NULL,
    [kvle_key]         NVARCHAR (255)   NOT NULL,
    [kvle_expiry]      BIGINT           NOT NULL,
    [kvle_interval]    BIGINT           NOT NULL,
    [kvle_value]       VARBINARY (MAX)  NOT NULL,
    [kvle_compressed]  BIT              NOT NULL,
    [kvle_creation]    BIGINT           NOT NULL,
    [kvle_parent_key0] NVARCHAR (255)   NULL,
    [kvle_parent_key1] NVARCHAR (255)   NULL,
    [kvle_parent_key2] NVARCHAR (255)   NULL,
    [kvle_parent_key3] NVARCHAR (255)   NULL,
    [kvle_parent_key4] NVARCHAR (255)   NULL,
    CONSTRAINT [pk_kvle] PRIMARY KEY CLUSTERED ([kvle_id]), 
    CONSTRAINT [uk_kvle] UNIQUE ([kvle_partition] ASC, [kvle_key] ASC), 
    CONSTRAINT [fk_kvle_parent0] FOREIGN KEY ([kvle_partition], [kvle_parent_key0]) REFERENCES [dbo].[kvl_cache_entries]([kvle_partition], [kvle_key]),
    CONSTRAINT [fk_kvle_parent1] FOREIGN KEY ([kvle_partition], [kvle_parent_key1]) REFERENCES [dbo].[kvl_cache_entries]([kvle_partition], [kvle_key]),
    CONSTRAINT [fk_kvle_parent2] FOREIGN KEY ([kvle_partition], [kvle_parent_key2]) REFERENCES [dbo].[kvl_cache_entries]([kvle_partition], [kvle_key]),
    CONSTRAINT [fk_kvle_parent3] FOREIGN KEY ([kvle_partition], [kvle_parent_key3]) REFERENCES [dbo].[kvl_cache_entries]([kvle_partition], [kvle_key]),
    CONSTRAINT [fk_kvle_parent4] FOREIGN KEY ([kvle_partition], [kvle_parent_key4]) REFERENCES [dbo].[kvl_cache_entries]([kvle_partition], [kvle_key])
);


GO
CREATE INDEX [ix_kvle_exp_part] ON [dbo].[kvl_cache_entries] ([kvle_expiry] DESC, [kvle_partition] ASC);


GO
CREATE INDEX [fk_kvle_parent0] ON [dbo].[kvl_cache_entries] ([kvle_partition], [kvle_parent_key0]);
CREATE INDEX [fk_kvle_parent1] ON [dbo].[kvl_cache_entries] ([kvle_partition], [kvle_parent_key1]);
CREATE INDEX [fk_kvle_parent2] ON [dbo].[kvl_cache_entries] ([kvle_partition], [kvle_parent_key2]);
CREATE INDEX [fk_kvle_parent3] ON [dbo].[kvl_cache_entries] ([kvle_partition], [kvle_parent_key3]);
CREATE INDEX [fk_kvle_parent4] ON [dbo].[kvl_cache_entries] ([kvle_partition], [kvle_parent_key4]);


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'kvl_cache_entries', @level2type = N'COLUMN', @level2name = N'kvle_compressed';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'kvl_cache_entries', @level2type = N'COLUMN', @level2name = N'kvle_creation';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'kvl_cache_entries', @level2type = N'COLUMN', @level2name = N'kvle_parent_key0';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'kvl_cache_entries', @level2type = N'COLUMN', @level2name = N'kvle_parent_key1';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'kvl_cache_entries', @level2type = N'COLUMN', @level2name = N'kvle_parent_key2';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'kvl_cache_entries', @level2type = N'COLUMN', @level2name = N'kvle_parent_key3';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'kvl_cache_entries', @level2type = N'COLUMN', @level2name = N'kvle_parent_key4';

