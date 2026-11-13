using CourseRegistration.ConsoleUi;
using CourseRegistration.Models;
using CourseRegistration.Services;

// The single course on offer: .NET201, which requires .NET101, with 3 seats.
var course = new CourseOffering(".NET201", capacity: 3, prerequisiteCourseCode: ".NET101");

// 10 users. The first 5 have completed the prerequisite (.NET101); the last 5 have not.
List<User> users =
[
    new("Alice", [".NET101"]),
    new("Bob",   [".NET101", "SQL100"]),
    new("Carol", [".NET101"]),
    new("David", [".NET101", "JS100"]),
    new("Emma",  [".NET101"]),
    new("Frank"),
    new("Grace", ["SQL100"]),
    new("Henry"),
    new("Isla",  ["JS100"]),
    new("Jack"),
];

var service = new EnrollmentService([course]);

new RegistrationConsole(service, course, users, Console.In, Console.Out).Run();
