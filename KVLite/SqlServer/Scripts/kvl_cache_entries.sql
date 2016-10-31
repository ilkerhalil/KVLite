CREATE TABLE [dbo].[kvl_cache_entries] (
    [kvle_partition]   NVARCHAR (255)  NOT NULL,
    [kvle_key]         NVARCHAR (255)  NOT NULL,
    [kvle_expiry]      BIGINT          NOT NULL,
    [kvle_interval]    BIGINT          NOT NULL,
    [kvle_value]       VARBINARY (MAX) NOT NULL,
    [kvle_compressed]  BIT             NOT NULL,
    [kvle_creation]    BIGINT          NOT NULL,
    [kvle_parent_key0] NVARCHAR (255)  NULL,
    [kvle_parent_key1] NVARCHAR (255)  NULL,
    [kvle_parent_key2] NVARCHAR (255)  NULL,
    [kvle_parent_key3] NVARCHAR (255)  NULL,
    [kvle_parent_key4] NVARCHAR (255)  NULL,
    PRIMARY KEY CLUSTERED ([kvle_partition] ASC, [kvle_key] ASC), 
    CONSTRAINT [fk_kvle_parent0] FOREIGN KEY ([kvle_partition], [kvle_parent_key0]) REFERENCES [dbo].[kvl_cache_entries]([kvle_partition], [kvle_key]),
    CONSTRAINT [fk_kvle_parent1] FOREIGN KEY ([kvle_partition], [kvle_parent_key1]) REFERENCES [dbo].[kvl_cache_entries]([kvle_partition], [kvle_key]),
    CONSTRAINT [fk_kvle_parent2] FOREIGN KEY ([kvle_partition], [kvle_parent_key2]) REFERENCES [dbo].[kvl_cache_entries]([kvle_partition], [kvle_key]),
    CONSTRAINT [fk_kvle_parent3] FOREIGN KEY ([kvle_partition], [kvle_parent_key3]) REFERENCES [dbo].[kvl_cache_entries]([kvle_partition], [kvle_key]),
    CONSTRAINT [fk_kvle_parent4] FOREIGN KEY ([kvle_partition], [kvle_parent_key4]) REFERENCES [dbo].[kvl_cache_entries]([kvle_partition], [kvle_key])
);


CREATE INDEX [ix_kvle_exp_part] ON [dbo].[kvl_cache_entries] ([kvle_expiry] DESC, [kvle_partition] ASC)
GO


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'crvn_kvl_entries', @level2type = N'COLUMN', @level2name = N'kvle_compressed';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'crvn_kvl_entries', @level2type = N'COLUMN', @level2name = N'kvle_creation';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'crvn_kvl_entries', @level2type = N'COLUMN', @level2name = N'kvle_parent_key0';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'crvn_kvl_entries', @level2type = N'COLUMN', @level2name = N'kvle_parent_key1';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'crvn_kvl_entries', @level2type = N'COLUMN', @level2name = N'kvle_parent_key2';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'crvn_kvl_entries', @level2type = N'COLUMN', @level2name = N'kvle_parent_key3';


GO
EXECUTE sp_addextendedproperty @name = N'MS_Description', @value = '', @level0type = N'SCHEMA', @level0name = N'dbo', @level1type = N'TABLE', @level1name = N'crvn_kvl_entries', @level2type = N'COLUMN', @level2name = N'kvle_parent_key4';

