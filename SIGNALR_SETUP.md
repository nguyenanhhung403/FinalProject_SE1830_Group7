# SignalR Real-Time Notification Setup Guide

This guide helps teammates set up the SignalR real-time notification system for the EV Warranty Management application.

## Overview

The SignalR implementation provides:
- Real-time dashboard updates when warranty claims are created/updated
- Notification bell with badge counter in the navbar
- Live chat system for warranty claims
- Role-based notifications (EVM Staff, SC Staff, SC Technicians)

## Prerequisites

Before running the application, ensure you have:
1. SQL Server database set up
2. Connection string configured in `appsettings.json`
3. All NuGet packages restored

## Database Setup

### Step 1: Create ClaimMessages Table

The SignalR chat functionality requires a `ClaimMessages` table in your database.

**Execute the SQL script:**
```bash
Execute the file: AddClaimMessagesTable.sql
```

This script will:
- Create the `ev.ClaimMessages` table
- Set up foreign key relationships to `WarrantyClaim` and `Users` tables
- Create indexes for performance
- Configure default values and constraints

**Important:** Make sure the following tables exist before running the script:
- `ev.WarrantyClaim` (singular, not plural)
- `ev.Users`

### Step 2: Update Database Context (Database First Approach)

Since this project uses **Database First** with scaffolding, you need to update the `EVWarrantyManagementContext` class.

**Option A: Re-scaffold the database context (Recommended)**
```bash
# Navigate to the DAL project directory
cd EVWarrantyManagement.DAL

# Scaffold the database (update with your connection string)
dotnet ef dbcontext scaffold "Your-Connection-String" Microsoft.EntityFrameworkCore.SqlServer --output-dir Models --context-dir . --context EVWarrantyManagementContext --force
```

**Option B: Manually add the DbSet**

If you prefer not to re-scaffold, manually add this property to `EVWarrantyManagementContext.cs`:

```csharp
public virtual DbSet<ClaimMessage> ClaimMessages { get; set; }
```

And add entity configuration in the `OnModelCreating` method:

```csharp
modelBuilder.Entity<ClaimMessage>(entity =>
{
    entity.HasKey(e => e.MessageId);

    entity.ToTable("ClaimMessages", "ev");

    entity.Property(e => e.Message)
        .IsRequired()
        .HasMaxLength(2000);

    entity.Property(e => e.Timestamp)
        .HasDefaultValueSql("(sysutcdatetime())");

    entity.Property(e => e.IsRead)
        .HasDefaultValue(false);

    entity.HasOne(d => d.WarrantyClaim)
        .WithMany(p => p.ClaimMessages)
        .HasForeignKey(d => d.ClaimId)
        .OnDelete(DeleteBehavior.Cascade);

    entity.HasOne(d => d.User)
        .WithMany()
        .HasForeignKey(d => d.UserId);
});
```

### Step 3: Build the Project

After setting up the database and updating the context:

```bash
dotnet build
```

You should see: `Build succeeded. 0 Error(s)`

## Verifying SignalR Setup

### 1. Check SignalR Hubs

Ensure these hub files exist:
- `EVWarrantyManagement/Hubs/NotificationHub.cs`
- `EVWarrantyManagement/Hubs/ChatHub.cs`

### 2. Check JavaScript Files

Verify these files exist in `wwwroot/js/`:
- `signalr-connection.js` - Connection manager
- `notification-manager.js` - Notification bell handler
- `dashboard-realtime.js` - Dashboard real-time updates
- `claims-realtime.js` - Claims page real-time updates
- `chat.js` - Chat functionality

### 3. Check Program.cs Configuration

Verify SignalR is registered in `Program.cs`:

```csharp
// Add SignalR service
builder.Services.AddSignalR();

// Register repositories and services
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Map hub endpoints
app.MapHub<NotificationHub>("/hubs/notification");
app.MapHub<ChatHub>("/hubs/chat");
```

## Testing SignalR

### Test Real-Time Dashboard Updates

1. Log in as **EVM Staff** and open the Dashboard page
2. In another browser/tab, log in as **SC Staff**
3. As SC Staff, create a new warranty claim
4. The EVM Staff dashboard should automatically update with the new claim
5. A toast notification should appear
6. The notification bell counter should increment

### Test Chat Functionality

1. Open a warranty claim's Details page
2. Scroll to the chat section
3. Send a message
4. If another user is viewing the same claim, they should see the message in real-time

## Troubleshooting

### Build Errors

**Error:** `'EVWarrantyManagementContext' does not contain a definition for 'ClaimMessages'`
- **Solution:** Make sure you completed Step 2 (Update Database Context)

### Connection Issues

**Error:** SignalR connection fails in browser console
- **Solution:** Check that hub endpoints are correctly mapped in `Program.cs`
- Verify the hub URLs match: `/hubs/notification` and `/hubs/chat`

### Database Errors

**Error:** Foreign key constraint error when creating messages
- **Solution:** Ensure `WarrantyClaim` and `Users` tables exist
- Verify the foreign key columns match the referenced tables

## Architecture

### SignalR Components

```
Client (Browser)
    ↓
JavaScript Client (signalr-connection.js)
    ↓
ASP.NET Core SignalR Hub (NotificationHub, ChatHub)
    ↓
Services (MessageService, NotificationService)
    ↓
Repositories (MessageRepository)
    ↓
Database (ClaimMessages table)
```

### Real-Time Flow

1. SC Staff creates a warranty claim
2. `Create.cshtml.cs` calls `NotificationHub`
3. Hub broadcasts to connected clients (EVM Staff)
4. JavaScript handler updates UI in real-time
5. Notification bell counter increments

## Support

For issues or questions about SignalR setup, contact the development team or refer to:
- Microsoft SignalR Documentation: https://docs.microsoft.com/aspnet/core/signalr
- Project repository issues page
