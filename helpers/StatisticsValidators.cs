using FluentValidation;
using HRAttendance.Api.Dtos;

namespace HRAttendance.Api.helpers;

public class PreviousYearStatisticsInputValidator : AbstractValidator<PreviousYearStatisticsInput>
{
    public PreviousYearStatisticsInputValidator()
    {
        RuleFor(x => x.PendingDecision).GreaterThanOrEqualTo(0).WithMessage("ارجاءات البت لا يمكن أن تكون بالسالب");
        RuleFor(x => x.TotalPreviousYearCases).GreaterThanOrEqualTo(0).WithMessage("إجمالي قضايا العام السابق لا يمكن أن يكون بالسالب");
        RuleFor(x => x.CompletedCases).GreaterThanOrEqualTo(0).WithMessage("عدد القضايا المنتهية لا يمكن أن يكون بالسالب");
        RuleFor(x => x.UnderInvestigation).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TechnicalOfficeSent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TechnicalOfficeUnderCopying).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BranchSent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BranchUnderCopying).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CentralAdministrations).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FollowUpCases).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SpatialVariableCases).GreaterThanOrEqualTo(0);
    }
}

public class CurrentYearStatisticsInputValidator : AbstractValidator<CurrentYearStatisticsInput>
{
    public CurrentYearStatisticsInputValidator()
    {
        RuleFor(x => x.TransferredFromBeginningOfYear).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedDuringMonth).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CompletedCases).GreaterThanOrEqualTo(0).WithMessage("عدد القضايا المنتهية لا يمكن أن يكون بالسالب");
        RuleFor(x => x.UnderInvestigation).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TechnicalOfficeSent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.TechnicalOfficeUnderCopying).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BranchSent).GreaterThanOrEqualTo(0);
        RuleFor(x => x.BranchUnderCopying).GreaterThanOrEqualTo(0);
        RuleFor(x => x.CentralAdministrations).GreaterThanOrEqualTo(0);
        RuleFor(x => x.FollowUpCases).GreaterThanOrEqualTo(0);
        RuleFor(x => x.SpatialVariableCases).GreaterThanOrEqualTo(0);
        RuleFor(x => x.ReceivedFromOtherProsecutions).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PendingDecision).GreaterThanOrEqualTo(0);

        // Hard rule: completed cases can never exceed the cases actually available this
        // month (transferred + received). This one IS rejected outright, unlike the
        // previous-year figures which may arrive already-inconsistent via Excel import
        // and are instead flagged for review (see StatisticsCalculationService).
        RuleFor(x => x)
            .Must(x => x.CompletedCases <= x.TransferredFromBeginningOfYear + x.ReceivedDuringMonth)
            .WithMessage("عدد القضايا المنتهية لا يمكن أن يتجاوز إجمالي القضايا المسجلة هذا العام")
            .WithName("CompletedCases");
    }
}

public class UpdateEmployeeStatisticsRequestValidator : AbstractValidator<UpdateEmployeeStatisticsRequest>
{
    public UpdateEmployeeStatisticsRequestValidator()
    {
        RuleFor(x => x.PreviousYear).NotNull().WithMessage("بيانات العام السابق مطلوبة");
        RuleFor(x => x.PreviousYear)
            .SetValidator(new PreviousYearStatisticsInputValidator())
            .When(x => x.PreviousYear is not null);

        RuleFor(x => x.CurrentYear).NotNull().WithMessage("بيانات العام الحالي مطلوبة");
        RuleFor(x => x.CurrentYear)
            .SetValidator(new CurrentYearStatisticsInputValidator())
            .When(x => x.CurrentYear is not null);

        RuleFor(x => x.LeaveReason).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
