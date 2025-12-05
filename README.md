# ExchangeMail (Caduceus Web App)

ExchangeMail is a modern, self-hosted web-based email client and server built with ASP.NET Core. It provides a comprehensive solution for sending, receiving, and managing emails with a focus on user experience and extensibility.

## Features

### 📧 Core Email Functionality
-   **Full SMTP Server**: Built-in SMTP server (`SmtpServer`) to receive emails directly.
-   **Send & Receive**: Complete support for composing and reading emails.
-   **Real-time Updates**: Uses SignalR to push new email notifications to the client instantly without refreshing.
-   **HTML Sanitization**: Secure rendering of email content using `HtmlSanitizerService`.

### 🗂 Organization & Management
-   **Mail Rules Engine**: Powerful rule matcher to automatically organize incoming mail (move to folders, mark as read, etc.).
-   **Folder Management**: Create, rename, and delete custom folders to organize your mailbox.
-   **PST Import**: Import existing email archives from PST files using the `PstImportService`.

### 🛡️ Security & Filtering
-   **Spam Protection**: Integrated `BasicJunkFilterService` to identify and filter junk mail.
-   **Block & Safe Lists**: Manage blocked senders and safe senders to control your inbox.
-   **Session-based Authentication**: Secure user access management.

### 👥 Contacts
-   **Contact Management**: Store and manage your personal address book.

## Screenshots

### Inbox View
![Inbox View](Images/inbox_view.png)

### Compose Email
![Compose Email](Images/compose_email.png)

### Settings & Rules
![Settings and Rules](Images/settings_rules.png)

## Tech Stack

-   **Framework**: ASP.NET Core 9.0 (MVC)
-   **Database**: SQLite (with Entity Framework Core)
-   **Real-time**: SignalR
-   **Frontend**: Razor Views, jQuery, Bootstrap
-   **SMTP**: SmtpServer library

## Getting Started

### Prerequisites
-   [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Installation

1.  Clone the repository:
    ```bash
    git clone https://github.com/yourusername/ExchangeMail.git
    cd ExchangeMail
    ```

2.  Restore dependencies:
    ```bash
    dotnet restore
    ```

### Running the Application

1.  Navigate to the Web project directory:
    ```bash
    cd ExchangeMail.Web
    ```

2.  Run the application:
    ```bash
    dotnet run
    ```

3.  Open your browser and navigate to `https://localhost:7152` (or the URL displayed in the console).

*Note: The application will automatically create and migrate the SQLite database (`exchangemail.db`) on the first run.*

## Architecture

The solution is structured into the following projects:

-   **ExchangeMail.Web**: The main ASP.NET Core MVC application containing Controllers, Views, and SignalR Hubs.
-   **ExchangeMail.Core**: Contains the core business logic, interfaces, domain models, and service implementations.
-   **ExchangeMail.Server**: (Optional) Standalone server component.
-   **ExchangeMail.Tests**: Unit tests for the application.

## License

[MIT License](LICENSE)
