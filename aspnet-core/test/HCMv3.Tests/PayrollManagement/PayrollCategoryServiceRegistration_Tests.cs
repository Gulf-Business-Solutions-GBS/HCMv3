using System;
using System.Linq;
using System.Reflection;
using HCMv3.PayrollManagement.Services;
using Shouldly;
using Xunit;

namespace HCMv3.Tests.PayrollManagement;

/// <summary>
/// Wave P9 -- DI regression guard for the four payroll CATEGORY services.
///
/// <para><b>Why this file exists.</b> <c>PayrollEngineService</c> receives
/// <see cref="IPayrollArrearCalculationService"/>, <see cref="IEmployeePaymentCalculationService"/>,
/// <see cref="IEmployeeOvertimePayCalculationService"/> and
/// <see cref="IEmployeeAttendancePayCalculationService"/> by OPTIONAL PROPERTY injection, falling back to a
/// Null object that throws. ABP's conventional registrar registers a class under
/// <c>WithService.DefaultInterfaces()</c>, and Castle's default-interface strategy matches an interface to a
/// class BY NAME. If one of the four implementations is ever renamed such that the strategy stops matching,
/// the class still registers (under its own concrete type), nothing fails at startup, and the engine keeps the
/// Null object -- every arrear/payment/overtime/attendance wage type then fails per employee. This is the same
/// class of failure that <c>WageTypeCalculatorRegistration_Tests</c> guards for
/// <see cref="IWageTypeCalculator"/>, and it has bitten this project before.</para>
/// </summary>
// ReSharper disable once InconsistentNaming
public class PayrollCategoryServiceRegistration_Tests : AppTestBase
{
    public static TheoryData<Type, Type> CategoryServices => new TheoryData<Type, Type>
    {
        { typeof(IPayrollArrearCalculationService), typeof(PayrollArrearCalculationService) },
        { typeof(IEmployeePaymentCalculationService), typeof(EmployeePaymentCalculationService) },
        { typeof(IEmployeeOvertimePayCalculationService), typeof(EmployeeOvertimePayCalculationService) },
        { typeof(IEmployeeAttendancePayCalculationService), typeof(EmployeeAttendancePayCalculationService) }
    };

    /// <summary>
    /// Each of the four interfaces must resolve, and must resolve to the REAL implementation -- not to
    /// <c>PayrollEngineService.NullPayrollCategoryService</c>.
    /// </summary>
    [Theory]
    [MemberData(nameof(CategoryServices))]
    public void Each_Category_Service_Interface_Should_Resolve_To_Its_Real_Implementation(
        Type serviceInterface,
        Type expectedImplementation)
    {
        object resolved = null;

        Should.NotThrow(() => resolved = LocalIocManager.IocContainer.Resolve(serviceInterface));

        resolved.ShouldNotBeNull();

        // The container may hand back a Castle interception proxy, so compare on the proxied class rather
        // than with ShouldBeOfType.
        var resolvedType = resolved.GetType();
        var isExpected = expectedImplementation.IsAssignableFrom(resolvedType);

        isExpected.ShouldBeTrue(
            $"{serviceInterface.Name} resolved to {resolvedType.FullName} instead of "
            + $"{expectedImplementation.FullName}. If this is the Null object, ABP conventional registration "
            + "stopped matching the implementation to its interface (Castle's DefaultInterfaces strategy "
            + "matches BY NAME), and every wage type routed to that category will fail per employee.");

        resolvedType.Name.ShouldNotContain(
            "Null",
            Case.Insensitive,
            $"{serviceInterface.Name} resolved to the Null fallback.");
    }

    /// <summary>
    /// The engine itself must come out of the container with all four properties replaced. This is the
    /// end-to-end assertion: it exercises the exact property-injection path a real payroll run uses, rather
    /// than just proving the interfaces are registered.
    /// </summary>
    [Fact]
    public void PayrollEngineService_Should_Have_All_Four_Category_Services_Injected()
    {
        var engine = LocalIocManager.Resolve<PayrollEngineService>();

        engine.ShouldNotBeNull();

        AssertInjected(engine.ArrearService, nameof(PayrollEngineService.ArrearService));
        AssertInjected(engine.PaymentService, nameof(PayrollEngineService.PaymentService));
        AssertInjected(engine.OvertimePayService, nameof(PayrollEngineService.OvertimePayService));
        AssertInjected(engine.AttendancePayService, nameof(PayrollEngineService.AttendancePayService));
    }

    private static void AssertInjected(object service, string propertyName)
    {
        service.ShouldNotBeNull($"PayrollEngineService.{propertyName} is null.");

        service.GetType().Name.ShouldNotContain(
            "Null",
            Case.Insensitive,
            $"PayrollEngineService.{propertyName} is still the Null fallback, so Castle property injection "
            + "did not run for it. Every wage type routed to that category will fail per employee with a "
            + "NotImplementedException, and the run will report those employees as failed.");
    }

    /// <summary>
    /// The self-maintaining half: every concrete implementation of the four category interfaces that lives in
    /// <c>HCMv3.Core</c> must be resolvable. Catches a future second implementation that never registers.
    /// </summary>
    [Fact]
    public void Every_Category_Service_Implementation_In_The_Core_Assembly_Should_Be_Registered()
    {
        var coreAssembly = typeof(IPayrollEngineService).GetTypeInfo().Assembly;

        var interfaces = new[]
        {
            typeof(IPayrollArrearCalculationService),
            typeof(IEmployeePaymentCalculationService),
            typeof(IEmployeeOvertimePayCalculationService),
            typeof(IEmployeeAttendancePayCalculationService)
        };

        foreach (var serviceInterface in interfaces)
        {
            var implementations = coreAssembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && serviceInterface.IsAssignableFrom(t))
                .ToList();

            implementations.Count.ShouldBe(
                1,
                $"Expected exactly one {serviceInterface.Name} implementation in HCMv3.Core, found: "
                + string.Join(", ", implementations.Select(t => t.FullName)));

            implementations[0].Name.ShouldBe(
                serviceInterface.Name.Substring(1),
                "The implementation must be named exactly like its interface without the leading 'I', or "
                + "Castle's DefaultInterfaces strategy will not register it under that interface.");
        }
    }

    /// <summary>
    /// <see cref="IPayrollWeekOffCalculator"/> is a wave-P9 helper shared by the overtime and payment
    /// services; if it fails to register, BOTH of them fail to construct.
    /// </summary>
    [Fact]
    public void The_Shared_WeekOff_Calculator_Should_Resolve()
    {
        IPayrollWeekOffCalculator calculator = null;

        Should.NotThrow(() => calculator = LocalIocManager.Resolve<IPayrollWeekOffCalculator>());

        calculator.ShouldNotBeNull();
    }
}
