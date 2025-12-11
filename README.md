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
![Login Screen Placeholder](DOCS/Images/LoginScreenPlaceholder.png)

### Inbox View
<img width="1595" height="994" alt="image" src="https://github.com/user-attachments/assets/3482e10d-7f8b-4012-89dc-4698514c80ea" />

### Today's Outlook
![Today's Outlook Placeholder](DOCS/Images/TodaysOutlookPlaceholder.png)


### Compose Email
<img width="1601" height="1002" alt="image" src="https://github.com/user-attachments/assets/b765d7b4-aaad-4039-a72f-6f14b7a73496" />


### Settings & Rules
<img width="1603" height="997" alt="image" src="https://github.com/user-attachments/assets/244cfa1e-20f9-4b6a-9e12-dcda56dea2d5" />


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
