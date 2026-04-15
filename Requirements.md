Transaction Dispute portal
---------------------------

Basic idea:
------------

* I would like to design a portal for customers to be able to dispute transactions on a banking system (Capitec). 
* Create a system (front end and back end) for customers to view transactions and dispute them.
* There must be a historic view of disputed transactions.
* I would love to simulate this system both on a computer as well as a mobile view

Flow of program
----------------

User:

* A user is met with a log in screen.
* If a user has log in details already(username(email address and password) it is collected from the database and log in is successfull.
* If a user does not have log in details, they are required to sign up( a sign up screen appears where user is asked to add the needed required information and once collected saved to the database), they are then able to log in.
* A user should see a list of historic various transactions in Rands(ZAR). Maybe ad a simulate trnsaction button that adds a list of simulated transactions.
* A user should be able to select a transaction and a pop up should appear where a user can select to dispte the transaction)
* If a user decide to dispute, a user should be able to select an option as to wy they are disputing this option, as well as an other option where a user should be able to enter a reason.
* Once a reason is selected, a user should be able to write a shot summary as to why they are dispution the transaction, to provide further details if needed.
* Once the dispute has been confirmed, a confirmation screen or icon should apprear so that user can know that it has been submitted successfuly.
* A ticket is then created and assigned to a Capitec employee.
* A user should then get an email or SMS, that states that the dispute has been received and they should get an unique incident reference number to allow them to follow up as well as an status, at this case(submitted/ pending review e.g.)
* A user should get an email/sms once a Capitec employee picks up the ticket and starts investigating, in the email it should state investigating/in progress.
* The same should happen once the case has been resolved, a user should get an email stating that it is resolved.
* Once a user is logged in and has disputed an transaction, they should have a dispted section, where they can see there disputed transactions, the statuses they are in as well as a history of disputed transations with theyr statuses.

Capitec Employee:

* A employee should be able to log into their own portal.(Login also received from a database)
* A New employee should be provided with login details as per company standards
* Once an employee has logged in, they are able to see the tickets on their name.
* Tickets are displayed from oldest to newest in order of priority as well as an emplyee is able to type in a unique reference number, that was provided to the customer, to bring up the information of a specific ticket.
* An Employee should be able to chance the status of an ticket from "Pending, to "In Progress", to "Resolved".
* When changing a ticket to status "In Progress" or "Resloved" an email should be sent to the customer to update them.
* When a a ticket is changed to "In progress", an employee should have the option to reqest a call with the customer if more information is needed.
* If this option is selected by the employee, on the email that the customer receives regardin the status update, it should highlight that the employee is requesting a call with them and the user should expect an call in the next 30 minutes.
* A user can then declre on the call if a better time would suit them.
* Once a ticket is resolved, the ticket is kept on a backlog for one year, in the employee portal un assigned but still detectable if unique number is typed in.
* A Employee, is ony able to see the User Name and Surname, Contact Number and account number and transactions.


General 
-------------
* Client sensitive information bused be hashed
* Ensure the service has auth for the endpoint requests
* Ensure mt service is runnable on Docker for both Front-End and Back-End
* Ensure that unit tests are covered for both Front-End and Back-End
* Ensure our service can Handle mltiple requests coming concurrently.
* Handle race conditions
* Secrets must not be exposed e.g. your DB credentials 
* For log-ins( both user and employee) Please ensure that multi-factor authentication has to be used.

Pre-requirements
----------------

* Production-grade (reflecting the quality and standards you would apply in a real-world environment).
* Includes a runnable Dockerfile to build and run the project.
* Accompanied by a README that explains how to build, run, and test the project.
* Use Asynchronous Programming (async/await) to ensure high- performance systems that don't block threads, especially when handelinh millions of transactions




Design
-------

Use the following to align to Capitec's brand:

*Please make the user interface as easy and simple to work with for users of all ages.

*Use a clean professional aesthetic featuring their primary Royal Blue(#2F70EF) for actions
*Use Alizarin Crimson(#E51718) for critical alerts or disputes.
*Brand Compliance: Use the specific hex codes for Capitec's Royal Blue and Alizarin Crimson to evoke a sense of familiarity.
*User Experience(UX): to avoid "clunky interfaces" by using a clean modal layout and card-based information.
*Security Cues: Incorporating a "Security Notice" and the ShieldCheck icon helps build user trust.
*Accessibility:: Use high-contrast text and clear labels. 

Back-end
--------
* Framework: ASP.NET Core 8.0
* Language: C#
* Database: SQL Server
* API Documentation: Swagger/OpenAPI
* ORM: Entity Framework Core
* Core Web API

Front-end
---------
* Framework: React 18
* Build Tool: Vite
* Language: TypeScript
* Styling: Tailwind CSS
* HTTP Client: Axios


Database
--------
Popi ACT Information is obfuscated unless specifically requested and then only allowed for a certain period of time.

* Database Schema: Use Entity Framework Core code-first with SQL Server. Key tables: Users, Employees, Transactions, Disputes, DisputeStatuses, Notifications. Include relationships, indexes, and optimistic locking for concurrency.
* MFA Implementation: Use ASP.NET Core Identity with TOTP (authenticator apps) for free implementation.
* Notification Service: Use AWS SES (free tier) for emails; mock SMS for development to avoid costs.
* File Uploads: Support attachments via AWS S3 (free tier) for dispute evidence.
* Admin Features: Add superuser role for system management (e.g., user oversight, reports).
* Audit Trails: Log all actions (e.g., status changes) in DisputeStatuses table for compliance.
* API Rate Limiting: Implement via ASP.NET Core middleware.
* Caching: Use in-memory caching for transaction lists; Redis if needed (free tier).
* Testing Frameworks: xUnit/NUnit for backend, Jest for frontend, Playwright for E2E.
* Deployment & Monitoring: GitHub Actions for CI/CD, Serilog for logging, AWS CloudWatch for monitoring.
* Data Seeding: Include seeded demo data for development.
* Error Handling: Expand global exception middleware with structured logging.

Standards
---------

* Use NuGet Oackages: Leverage standard tools like AutoMapper for mapping entities to DTO's and FluentValidation to eep the validation logic clean
* Global Exception Handling: Implement a Global Exception Middleware in my API to ensure the app never crashes and always returns a consistent error format

Key Component Structure
-----------------------

* Handle Server State(data from my C# API) seperatly from UI State(models,filters)
* TransactionList.js: Fetches and displaus the user's recent history using the React useEffect hook.
* DisputeForm.js: A controlled torm for selecting a reason(e.g., "Unauthorised", "Incorrect Amount") and submitting to your .NET backend
* DisputeHistory.js: A dedicated view showing the status(Pending, In Progress, Resolved) of previous claims.

Project Elevation
-----------------

* Tailwind CSS: use this for modern, responsive look without heavy CSS files
* Error Boundries: Wrap my components in an Error Boundry to prevent the entire app from crashing if one API Call fails.
* Lucid Icons: Use a library like lucide-react for a polished banking icons(e.g., a shield icon for disputes or a clock for history).
* CORS Configuration: Remember that my C# API must be configured for CORS to allow my React app to talk to it
 

Test methods
------------
* Use Swagger so interviewer can test endpoint immediately
* Unit Testing(TDD) XUnit or NUnit to test dispute logic
* Unit Testing for both Front-End and Back-End.

Project Structure Recommendations
---------------------------------
* four layers seperation

* Capitec.Dispute.Domain: Contains core entities like Dispute and Transaction, and business enums(e.g. DisputeStatus)
* Capitec.Dispute.Application: Holds the business logic, interfaces and DTOs ( Data Transfer Objects). This is where the "Service" layer lives
* Capitec.Dispute.Infrastructure: Implement data access using Entity Framework Core and handles external concerns like logging or email 
* Capitec.Dispute.API: The entry point. This ASP.Net Core Web API exposes the endpoints for my front-end

Summary
---------

2. High-Level Architecture
[ Frontend (React) ]
          |
          v
[ Gateway API (BFF) ]
          |
          v
[ Data Access API ]
          |
          v
[ SQLite Database ]

Architectural Principles

Separation of Concerns
Single Responsibility per API
Stateless APIs
Horizontal scalability
API-first design


3. System Components
3.1 Frontend (Client Application)
Technology

React + TypeScript
REST communication via HTTPS
No direct database access
Communicates only with Gateway API

Responsibilities

UI rendering
Client-side validation
Authentication token handling
User experience orchestration


3.2 Gateway API (Backend-for-Frontend)
Technology

C# .NET 7/8 REST API
Stateless
JWT-based authentication
Rate limiting & request validation

Responsibilities

Acts as a single entry point for frontend
Aggregates data from Data Access API
Applies:

Business orchestration
Security policies
Request validation


Prevents frontend from coupling to internal data models

Key Benefits

Protects internal APIs
Enables API versioning
Simplifies frontend logic
Scales independently


3.3 Data Access API (Core Backend)
Technology

C# .NET 7/8
Entity Framework Core
SQLite
Clean Architecture (Domain / Application / Infrastructure)

Responsibilities

Core business logic
Database transactions
Slot availability computation
Appointment lifecycle management
Concurrency control

Not exposed to frontend directly
Stateless
JWT-based authentication
Rate limiting & request validation

Responsibilities

Acts as a single entry point for frontend
Aggregates data from Data Access API
Applies:

Business orchestration
Security policies
Request validation


Prevents frontend from coupling to internal data models

Key Benefits

Protects internal APIs
Enables API versioning
Simplifies frontend logic
Scales independently


3.3 Data Access API (Core Backend)
Technology

C# .NET 7/8
Entity Framework Core
SQLite
Clean Architecture (Domain / Application / Infrastructure)

Responsibilities

Core business logic
Database transactions
Slot availability computation
Appointment lifecycle management
Concurrency control

Not exposed to frontend directly


