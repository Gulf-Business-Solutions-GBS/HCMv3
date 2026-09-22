using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Services;
using Abp.Domain.Uow;
using HCMv3.PersonnelAdministration;
using HCMv3.Recruitment;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Storage;

/// <summary>
/// Moves images still held in the old varbinary columns (<c>Employee.Photo</c>, <c>Applicant.Photo</c>,
/// <c>EmployeeSignature.Signature</c>) onto disk, one batch at a time, for the tenant the unit of work runs as.
///
/// <para>
/// Those columns hold everything written before images moved to the file store, and anything the legacy
/// migrator copies in afterwards (it copies by column name). Each moved row gets its path set and its bytes
/// emptied, so running again is harmless and a half-finished run simply continues next time.
/// </para>
///
/// <para>
/// If a row already has a path, a newer image was saved after the move began; the old bytes are then dropped
/// rather than overwriting it. Soft-deleted rows are moved too: they can be restored, and their bytes would
/// otherwise be lost when the old columns are finally dropped.
/// </para>
/// </summary>
public interface ILegacyImageMover : IDomainService
{
    /// <summary>Moves up to <paramref name="batchSize"/> images of each kind; returns how many were moved.</summary>
    Task<int> MoveBatchAsync(int batchSize);
}

/// <inheritdoc cref="ILegacyImageMover"/>
public class LegacyImageMover : HCMv3DomainServiceBase, ILegacyImageMover
{
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<Applicant> _applicantRepository;
    private readonly IRepository<EmployeeSignature> _signatureRepository;
    private readonly IEntityImageStore _imageStore;

    public LegacyImageMover(
        IRepository<Employee> employeeRepository,
        IRepository<Applicant> applicantRepository,
        IRepository<EmployeeSignature> signatureRepository,
        IEntityImageStore imageStore)
    {
        _employeeRepository = employeeRepository;
        _applicantRepository = applicantRepository;
        _signatureRepository = signatureRepository;
        _imageStore = imageStore;
    }

    public async Task<int> MoveBatchAsync(int batchSize)
    {
        var moved = 0;

        using (CurrentUnitOfWork.DisableFilter(AbpDataFilters.SoftDelete))
        {
            foreach (var employee in await _employeeRepository.GetAll()
                         .Where(e => e.LegacyPhoto != null).Take(batchSize).ToListAsync())
            {
                if (employee.PhotoPath == null)
                {
                    employee.PhotoPath = await _imageStore.ReplaceAsync(
                        EntityImageKinds.EmployeePhoto, employee.TenantId, employee.Id, null, employee.LegacyPhoto);
                }

                employee.LegacyPhoto = null;
                moved++;
            }

            foreach (var applicant in await _applicantRepository.GetAll()
                         .Where(a => a.LegacyPhoto != null).Take(batchSize).ToListAsync())
            {
                if (applicant.PhotoPath == null)
                {
                    applicant.PhotoPath = await _imageStore.ReplaceAsync(
                        EntityImageKinds.ApplicantPhoto, applicant.TenantId, applicant.Id, null, applicant.LegacyPhoto);
                }

                applicant.LegacyPhoto = null;
                moved++;
            }

            foreach (var signature in await _signatureRepository.GetAll()
                         .Where(s => s.LegacySignature != null).Take(batchSize).ToListAsync())
            {
                if (signature.SignaturePath == null)
                {
                    signature.SignaturePath = await _imageStore.ReplaceAsync(
                        EntityImageKinds.EmployeeSignature, signature.TenantId, signature.Id, null, signature.LegacySignature);
                }

                signature.LegacySignature = null;
                moved++;
            }

            await CurrentUnitOfWork.SaveChangesAsync();
        }

        return moved;
    }
}
