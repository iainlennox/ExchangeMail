# ExchangeMail (Caduceus Web App)

ExchangeMail is a modern, self-hosted web-based email client and server built with ASP.NET Core. It provides a comprehensive solution for sending, receiving, and managing emails with a focus on user experience and extensibility.

## Motivation

This project was born out of the need to find a viable, self-hosted alternative to Microsoft Exchange Server On-Premises. With the release of [Exchange Server Subscription Edition (SE)](https://techcommunity.microsoft.com/blog/exchange/exchange-server-subscription-edition-se-is-now-available/4424924), Microsoft has shifted to a subscription-based licensing model, moving away from the traditional one-time purchase.

As noted by [CodeTwo](https://www.codetwo.com/admins-blog/exchange-server-subscription-edition/):
> "It’s no longer a one-time purchase with a strongly suggested upgrade required every few years when the server version reaches end of life. The only way to have the new Exchange Server with a free license is to use it in a hybrid environment... The upside is that you won’t have to migrate your underlying on-premises server to prevent Exchange Online from “sabotaging” your mail flow."

ExchangeMail aims to fill this gap for users who prefer a standalone, perpetual, and self-controlled email infrastructure without recurring subscription fees or forced hybrid dependencies.

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
-   **Two-Factor Authentication (2FA)**: Secure your account with TOTP-based 2FA (compatible with Google/Microsoft Authenticator).
-   **Admin Password Reset**: administrators can reset user passwords directly from the user management dashboard.
-   **Spam Protection**: Integrated `BasicJunkFilterService` to identify and filter junk mail.
-   **Block & Safe Lists**: Manage blocked senders and safe senders to control your inbox.
-   **Session-based Authentication**: Secure user access management.

### 👥 Contacts
-   **Contact Management**: Store and manage your personal address book.

### ⚙️ User Experience & Customization
-   **Smart Inbox Management**:
    -   **Sorting & Filtering**: Sort emails by Date, Sender, or Subject, and filter by Unread or Urgent status using the new intuitive toolbar.
    -   **Focused Inbox**: Automatically separates important emails from bulk/other messages using a "Focused" and "Other" tab system.
    -   **Dark Mode**: Fully supported dark mode theme that respects system settings and user preference.

### 🤖 AI Features
-   **Today's Outlook**: Get a daily briefing with a summary of your schedule and important unread emails, including weather updates.
-   **Email Summarization**: Quickly grasp the content of long emails with AI-generated summaries.
-   **Auto-Labeling**: Intelligent tagging of emails based on content (Work, Personal, Urgent, etc.) and detection of potential security threats.
-   **AI Drafting**: Comprehensive AI assistance for drafting professional email replies.

## Screenshots

### Login Screen
<img width="1710" height="1004" alt="image" src="https://github.com/user-attachments/assets/d7b6c790-0e32-4e80-bc17-6afe26a91215" />

### Inbox View
<img width="1718" height="962" alt="image" src="https://github.com/user-attachments/assets/bd7e7b23-fe57-4bba-a6a7-f6147aaf8845" />

### Today's Outlook
<img width="1711" height="1002" alt="image" src="https://github.com/user-attachments/assets/c8bf6156-eeff-4deb-9a7e-fef6c707a32e" />

### Compose Email
<img width="1716" height="999" alt="image" src="https://github.com/user-attachments/assets/b466c92a-72dd-4b89-b08b-b2fad5070def" />

### Settings
<img width="1719" height="1006" alt="image" src="https://github.com/user-attachments/assets/3e1604fa-cc3b-452a-937f-40bbf1c06d18" />

### Rules
<img width="1715" height="1004" alt="image" src="https://github.com/user-attachments/assets/a68ff926-9947-43b3-89e5-8c52b586dd09" />



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
