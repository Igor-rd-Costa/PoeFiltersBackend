

using Microsoft.EntityFrameworkCore;

public class PoEFiltersContext : DbContext
{

    public PoEFiltersContext(DbContextOptions<PoEFiltersContext> options)
        : base(options)
    {

    }
}