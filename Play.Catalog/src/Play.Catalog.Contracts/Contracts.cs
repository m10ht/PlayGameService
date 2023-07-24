using System;

namespace Play.Catalog.Contratcts
{
    public record CatalogItemCreated(
        Guid ItemId,
        string Name,
        string Description);

    public record CatalogItemUpdated(
        Guid ItemId,
        string Name,
        string Description);

    public record CatelogItemDeleted(Guid ItemId);
}
