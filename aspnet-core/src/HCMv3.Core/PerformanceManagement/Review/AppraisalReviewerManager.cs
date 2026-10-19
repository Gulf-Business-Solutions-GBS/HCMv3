using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PerformanceManagement.Review;

/// <inheritdoc cref="IAppraisalReviewerManager"/>
public class AppraisalReviewerManager : HCMv3DomainServiceBase, IAppraisalReviewerManager
{
    private readonly IRepository<PAEmployeeReviewer> _reviewerRepository;
    private readonly IRepository<PAEmployeeDetail> _detailRepository;
    private readonly IRepository<PAEmployeeRating> _ratingRepository;
    private readonly IRepository<FormType> _formTypeRepository;
    private readonly IRepository<EmployeeManager> _employeeManagerRepository;
    private readonly IRepository<EmployeeRelationship> _employeeRelationshipRepository;

    public AppraisalReviewerManager(
        IRepository<PAEmployeeReviewer> reviewerRepository,
        IRepository<PAEmployeeDetail> detailRepository,
        IRepository<PAEmployeeRating> ratingRepository,
        IRepository<FormType> formTypeRepository,
        IRepository<EmployeeManager> employeeManagerRepository,
        IRepository<EmployeeRelationship> employeeRelationshipRepository)
    {
        _reviewerRepository = reviewerRepository;
        _detailRepository = detailRepository;
        _ratingRepository = ratingRepository;
        _formTypeRepository = formTypeRepository;
        _employeeManagerRepository = employeeManagerRepository;
        _employeeRelationshipRepository = employeeRelationshipRepository;
    }

    public async Task<PAEmployeeReviewer> AddReviewerAsync(
        PAEmployee appraisal,
        int reviewerEmployeeId,
        bool isPeer,
        int? peerAddedById,
        AppraisalStepSetting setting)
    {
        var reviewer = new PAEmployeeReviewer
        {
            TenantId = appraisal.TenantId,
            PAEmployeeId = appraisal.Id,
            EmployeeId = reviewerEmployeeId,
            IsPeer = isPeer,
            PeerAddedById = peerAddedById
        };

        await ValidateAsync(reviewer, appraisal, setting);

        // Saved for its id: the rating lines below point at it.
        reviewer.Id = await _reviewerRepository.InsertAndGetIdAsync(reviewer);

        await FillReviewAsync(reviewer);

        return reviewer;
    }

    /// <summary>
    /// Legacy <c>PAEmployeeReviewer.PerformValidation</c>, in its order: the duplicate, then the form type's
    /// limit, then the employee's own peer allowance.
    /// </summary>
    public Task ValidateAsync(PAEmployeeReviewer reviewer, PAEmployee appraisal, AppraisalStepSetting setting)
    {
        var others = _reviewerRepository.GetAll()
            .Where(r => r.PAEmployeeId == appraisal.Id && r.Id != reviewer.Id)
            .ToList();

        if (others.Any(r => r.EmployeeId == reviewer.EmployeeId))
        {
            throw new UserFriendlyException(L("PersonAlreadyExistsAsReviewer"));
        }

        var maxReviewerLimit = _formTypeRepository.GetAll()
            .Where(f => f.Id == appraisal.FormTypeId)
            .Select(f => f.MaxReviewerLimit)
            .FirstOrDefault();

        // Zero means "no limit", as in legacy.
        if (maxReviewerLimit != 0 && others.Count + 1 > maxReviewerLimit)
        {
            throw new UserFriendlyException(L("MaxReviewerLimitReached", maxReviewerLimit));
        }

        // The allowance covers only the peers the EMPLOYEE nominates. Somebody else's additions -- the
        // approver's, the automatic ones -- do not use it up. Legacy counted the new row as one whatever it
        // was, and excluded the employee's own reviewer row from the existing count.
        if (setting != null
            && setting.CanAddPeer
            && reviewer.PeerAddedById.HasValue
            && reviewer.PeerAddedById.Value == appraisal.EmployeeId)
        {
            var employeePeerCount = others.Count(r => r.EmployeeId != appraisal.EmployeeId
                                                      && r.PeerAddedById == appraisal.EmployeeId
                                                      && r.IsPeer) + 1;

            if (setting.AllowedPeerCount < employeePeerCount)
            {
                throw new UserFriendlyException(L("EmployeeCannotAddMorePeersThan", setting.AllowedPeerCount));
            }
        }

        return Task.CompletedTask;
    }

    public async Task<List<PAEmployeeReviewer>> AddStepReviewersAsync(
        PAEmployee appraisal,
        AppraisalStepSetting setting,
        IReadOnlyCollection<int> approverEmployeeIds)
    {
        var added = new List<PAEmployeeReviewer>();

        if (appraisal == null || setting == null || approverEmployeeIds == null)
        {
            return added;
        }

        var today = DateTime.Today;

        // Read once: legacy re-read Employee.CurrentManager inside the approver loop, but within one
        // assignment it can only ever give the same answer.
        var currentManagerId = setting.AutoAddManager
            ? _employeeManagerRepository.GetAll()
                .Where(m => m.EmployeeId == appraisal.EmployeeId && m.StartDate <= today && m.EndDate >= today)
                .OrderBy(m => m.Id)
                .Select(m => (int?)m.ManagerId)
                .FirstOrDefault()
            : null;

        var relatedEmployeeIds = new List<int>();
        if (setting.AutoAddRelationShip)
        {
            var relationshipTypeIds = (setting.Relationships ?? new List<AppraisalStepSettingRelationship>())
                .Select(r => r.RelationshipTypeId)
                .ToList();

            if (relationshipTypeIds.Count > 0)
            {
                relatedEmployeeIds = _employeeRelationshipRepository.GetAll()
                    .Where(r => r.EmployeeId == appraisal.EmployeeId
                                && r.RelatedEmployeeId > 0
                                && r.StartDate <= today
                                && r.EndDate >= today
                                && relationshipTypeIds.Contains(r.RelationshipTypeId))
                    .OrderBy(r => r.Id)
                    .Select(r => r.RelatedEmployeeId)
                    .ToList();
            }
        }

        // Per approver, as legacy looped: "added by" records which approver's assignment brought each
        // reviewer in.
        foreach (var approverId in approverEmployeeIds)
        {
            if (setting.CanEmployeeSelfRate)
            {
                await AddIfAbsentAsync(appraisal, appraisal.EmployeeId, isPeer: false, approverId, setting, added);
            }

            if (setting.CanAddRatings)
            {
                await AddIfAbsentAsync(appraisal, approverId, isPeer: false, approverId, setting, added);
            }

            if (currentManagerId.HasValue)
            {
                await AddIfAbsentAsync(appraisal, currentManagerId.Value, isPeer: true, approverId, setting, added);
            }

            foreach (var relatedEmployeeId in relatedEmployeeIds)
            {
                await AddIfAbsentAsync(appraisal, relatedEmployeeId, isPeer: true, approverId, setting, added);
            }
        }

        return added;
    }

    /// <summary>
    /// Legacy checked "already on the list?" before creating the row, so an automatic addition of somebody
    /// already reviewing is skipped quietly rather than refused. The remaining checks -- the form type's
    /// reviewer limit above all -- still apply and still throw, as they did: legacy saved the row through the
    /// same validation, so an assignment that overfills the form failed outright.
    /// </summary>
    private async Task AddIfAbsentAsync(
        PAEmployee appraisal,
        int employeeId,
        bool isPeer,
        int addedById,
        AppraisalStepSetting setting,
        List<PAEmployeeReviewer> added)
    {
        var exists = _reviewerRepository.GetAll()
            .Any(r => r.PAEmployeeId == appraisal.Id && r.EmployeeId == employeeId);

        if (exists)
        {
            return;
        }

        added.Add(await AddReviewerAsync(appraisal, employeeId, isPeer, addedById, setting));
    }

    /// <summary>Legacy <c>FillReview</c>: one empty rating line per KPI for the new reviewer.</summary>
    private async Task FillReviewAsync(PAEmployeeReviewer reviewer)
    {
        var detailIds = _detailRepository.GetAll()
            .Where(d => d.PAEmployeeId == reviewer.PAEmployeeId)
            .Select(d => d.Id)
            .ToList();

        var existing = _ratingRepository.GetAll()
            .Where(r => r.PAEmployeeReviewerId == reviewer.Id)
            .Select(r => r.PAEmployeeDetailId)
            .ToList();

        foreach (var detailId in detailIds.Where(id => !existing.Contains(id)))
        {
            await _ratingRepository.InsertAsync(new PAEmployeeRating
            {
                TenantId = reviewer.TenantId,
                PAEmployeeId = reviewer.PAEmployeeId,
                PAEmployeeReviewerId = reviewer.Id,
                PAEmployeeDetailId = detailId
            });
        }
    }
}
