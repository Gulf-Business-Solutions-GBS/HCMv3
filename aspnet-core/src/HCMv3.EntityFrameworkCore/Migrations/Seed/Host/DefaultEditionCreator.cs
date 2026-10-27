using System.Linq;
using Microsoft.EntityFrameworkCore;
using HCMv3.Editions;
using HCMv3.EntityFrameworkCore;

namespace HCMv3.Migrations.Seed.Host;

public class DefaultEditionCreator
{
    private readonly HCMv3DbContext _context;

    public DefaultEditionCreator(HCMv3DbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        CreateEditions();
    }

    private void CreateEditions()
    {
        var defaultEdition = _context.Editions.IgnoreQueryFilters().FirstOrDefault(e => e.Name == EditionManager.DefaultEditionName);
        if (defaultEdition == null)
        {
            defaultEdition = new SubscribableEdition { Name = EditionManager.DefaultEditionName, DisplayName = EditionManager.DefaultEditionName };
            _context.Editions.Add(defaultEdition);
            _context.SaveChanges();

            /* Add desired features to the standard edition, if wanted... */
        }

        /* Chat is NOT switched on here. The chat features default to off (AppFeatureProvider), so a new
           database starts without chat, and without one tenant's users being able to message another's.
           A host administrator turns it on per edition or per tenant under Features. */
    }
}

