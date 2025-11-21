# ST10313401_PROG6212_POEPART3
FINAL POE PROG6212

YouTube link: https://youtu.be/ZmPVnB7u10s
GitHub: https://github.com/Wu-Cindy-GuanYing/ST10313401_PROG6212_POEPART3.git

Contract Monthly Claim System
A comprehensive ASP.NET Core web application for managing monthly claims submission, approval, and payroll processing for contract lecturers in educational institutions.

🚀 Overview
The Contract Monthly Claim System (CMCS) is designed to streamline the process of monthly claims submission by contract lecturers, with built-in approval workflows for program coordinators and academic managers, and comprehensive reporting capabilities for HR departments.

👥 User Roles & Features
1. Lecturers
Submit monthly claims with automatic rate calculation

Upload supporting documents (PDF, DOCX, XLSX)

Track claim status and history

View personal claim statistics

2. Program Coordinators
Review and approve/reject pending claims

Monitor claims requiring first-level approval

View daily approval statistics

3. Academic Managers
Final review and approval of coordinator-approved claims

Access comprehensive approval analytics

Monthly approval rate tracking

4. HR Department
Manage lecturer profiles and rates

Create and manage staff accounts

Generate comprehensive reports

Process payroll and generate invoices

System-wide analytics and monitoring

🛠️ Technical Stack
Framework: ASP.NET Core MVC

Authentication: Identity Framework with Role-based Authorization

Database: Entity Framework Core with SQL Server

Session Management: Custom session extensions

File Handling: Multi-file upload with validation

Reporting: PDF generation with iTextSharp

Logging: Structured logging with ILogger

📋 Core Controllers
AccountController
User authentication and session management

Role-based login redirection

Secure logout with session cleanup

ClaimController
Claims submission with automatic calculations

File upload validation (10MB max, PDF/DOCX/XLSX)

Lecturer profile matching logic

Comprehensive error handling

ApprovalController
Two-tier approval workflow (Coordinator → Manager)

Role-based access control

Approval statistics and analytics

HRController
User management for all roles

Lecturer rate and profile management

Comprehensive reporting system

Payroll processing and invoice generation

🔒 Security Features
Role-based authorization

Anti-forgery token validation

Secure file upload validation

Session-based authentication

Password hashing with Identity

Email confirmation requirements

📊 Reporting System
Available Reports
Monthly Invoices: Summary invoices for individual lecturers

Payroll Reports: Comprehensive payroll data by period

Claims Reports: Filterable claims data with status tracking

Lecturer Reports: Performance and earnings analytics

Staff Reports: System user management overview

🗂️ File Structure
text
Controllers/
├── AccountController.cs      # Authentication & session management
├── ClaimController.cs        # Claims submission & management
├── ApprovalController.cs     # Two-tier approval workflow
├── HRController.cs          # HR management & reporting
├── HomeController.cs        # Role-based redirection
└── TestController.cs        # Development utilities
🚦 Workflow
Lecturer Submission

Login → Submit claim with hours → Automatic calculation → File upload → Submission

Approval Process

Coordinator reviews → Approve/Reject → Manager final review → Approval

HR Processing

Generate invoices → Process payroll → Analytics & reporting

🔧 Configuration
Session Management
The system uses custom session extensions for:

User role persistence

Lecturer profile matching

Authentication state tracking

File Upload
Maximum file size: 10MB

Allowed formats: PDF, DOCX, XLSX

Secure filename generation

Virus scanning recommended

Database Models
Key entities include:

CMCSUser (Identity extension)

Lecturer (contract details)

Claim (monthly submissions)

ClaimItem (line items)

Document (supporting files)

📈 Key Features
Automated Calculations
Automatic hourly rate application

Total amount calculation

Hours validation (0.25 - 180 hours)

Smart Lecturer Matching
Email-based matching from Identity

Name-based fallback matching

Development fallback to first active lecturer

Comprehensive Validation
Business rule enforcement

File type and size validation

Lecturer status checking

Rate validation

🎯 Usage Scenarios
Educational Institutions
University contract lecturer management

College adjunct faculty claims

Training organization contractor payments

Business Applications
Freelancer monthly billing

Contractor time tracking

Project-based claim submissions

🔮 Future Enhancements
Email notifications

Mobile-responsive UI

API endpoints for integration

Advanced analytics dashboard

Bulk operations for HR

Digital signatures
