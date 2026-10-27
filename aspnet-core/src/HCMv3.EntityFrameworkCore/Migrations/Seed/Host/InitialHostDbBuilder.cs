using HCMv3.EntityFrameworkCore;

namespace HCMv3.Migrations.Seed.Host;

public class InitialHostDbBuilder
{
    private readonly HCMv3DbContext _context;

    public InitialHostDbBuilder(HCMv3DbContext context)
    {
        _context = context;
    }

    public void Create()
    {
        new DefaultEditionCreator(_context).Create();
        new DefaultLanguagesCreator(_context).Create();
        new HostRoleAndUserCreator(_context).Create();
        new DefaultSettingsCreator(_context).Create();

        _context.SaveChanges();
    }
}

