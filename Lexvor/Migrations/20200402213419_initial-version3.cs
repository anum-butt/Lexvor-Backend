using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Lexvor.Migrations
{
    public partial class initialversion3 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    UserName = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Devices",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    DeviceType = table.Column<int>(nullable: false),
                    ImageUrl = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    Available = table.Column<bool>(nullable: false),
                    Archived = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InviteCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Code = table.Column<string>(nullable: true),
                    Used = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InviteCodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    AccountId = table.Column<string>(nullable: true),
                    AttemptDate = table.Column<DateTime>(nullable: false),
                    IP = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Success = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlanTypes",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    PlanDetails = table.Column<string>(nullable: true),
                    MonthlyCost = table.Column<int>(nullable: false),
                    InitiationFee = table.Column<int>(nullable: false),
                    SortOrder = table.Column<int>(nullable: false),
                    Archived = table.Column<bool>(nullable: false),
                    LastModified = table.Column<DateTime>(nullable: false),
                    ExternalId = table.Column<string>(nullable: true),
                    DisplayOnPublicPages = table.Column<bool>(nullable: false),
                    EnableDeviceOptions = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settings",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(nullable: true),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockedSims",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    ICCNumber = table.Column<string>(nullable: true),
                    Available = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockedSims", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Title = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    AccountId = table.Column<string>(nullable: true),
                    ShowOnce = table.Column<bool>(nullable: false),
                    ExpirationDate = table.Column<DateTime>(nullable: false),
                    Shown = table.Column<bool>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false),
                    Color = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Verbiage",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    LocationCode = table.Column<string>(nullable: true),
                    HTMLText = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Verbiage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WebHookObjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DateReceived = table.Column<DateTime>(nullable: false),
                    Payload = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WebHookObjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    RoleId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockedDevice",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DeviceId = table.Column<Guid>(nullable: true),
                    IMEI = table.Column<string>(nullable: true),
                    Available = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockedDevice", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockedDevice_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DiscountCodes",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Code = table.Column<string>(nullable: false),
                    NewMonthlyCost = table.Column<int>(nullable: false),
                    NewInitiationFee = table.Column<int>(nullable: false),
                    OneTimeUse = table.Column<bool>(nullable: false),
                    OneTimeUsePerUser = table.Column<bool>(nullable: false),
                    StartDate = table.Column<DateTime>(nullable: false),
                    EndDate = table.Column<DateTime>(nullable: false),
                    PlanTypeId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscountCodes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscountCodes_PlanTypes_PlanTypeId",
                        column: x => x.PlanTypeId,
                        principalTable: "PlanTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlanTypeDevices",
                columns: table => new
                {
                    DeviceId = table.Column<Guid>(nullable: false),
                    PlanTypeId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanTypeDevices", x => new { x.PlanTypeId, x.DeviceId });
                    table.ForeignKey(
                        name: "FK_PlanTypeDevices_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PlanTypeDevices_PlanTypes_PlanTypeId",
                        column: x => x.PlanTypeId,
                        principalTable: "PlanTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DeviceId = table.Column<Guid>(nullable: true),
                    OptionGroup = table.Column<string>(nullable: true),
                    OptionValue = table.Column<string>(nullable: true),
                    Surcharge = table.Column<int>(nullable: false),
                    StockedDeviceId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceOptions_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DeviceOptions_StockedDevice_StockedDeviceId",
                        column: x => x.StockedDeviceId,
                        principalTable: "StockedDevice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserDevices",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PlanId = table.Column<Guid>(nullable: false),
                    IMEI = table.Column<string>(nullable: true),
                    Requested = table.Column<DateTime>(nullable: true),
                    Shipped = table.Column<DateTime>(nullable: true),
                    IsActive = table.Column<bool>(nullable: false),
                    BYOD = table.Column<bool>(nullable: false),
                    ReturnRequested = table.Column<DateTime>(nullable: true),
                    ReturnApproved = table.Column<DateTime>(nullable: true),
                    StockedDeviceId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDevices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDevices_StockedDevice_StockedDeviceId",
                        column: x => x.StockedDeviceId,
                        principalTable: "StockedDevice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    AccountId = table.Column<string>(nullable: true),
                    CustomerType = table.Column<int>(nullable: false),
                    ProfileType = table.Column<int>(nullable: false),
                    ExternalCustomerId = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    Phone = table.Column<string>(nullable: true),
                    IDVerifyStatus = table.Column<int>(nullable: false),
                    IDVerifyStatusDescription = table.Column<string>(nullable: true),
                    BillingAddressId = table.Column<Guid>(nullable: true),
                    ShippingAddressId = table.Column<Guid>(nullable: true),
                    LastModified = table.Column<DateTime>(nullable: false),
                    DateJoined = table.Column<DateTime>(nullable: false),
                    ForceReaffirmation = table.Column<bool>(nullable: false),
                    BillingCycleStart = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Profiles_AspNetUsers_AccountId",
                        column: x => x.AccountId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AccountCredits",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: false),
                    Amount = table.Column<int>(nullable: false),
                    TimesToApply = table.Column<int>(nullable: false),
                    Reason = table.Column<string>(nullable: true),
                    ApplicableToInitiation = table.Column<bool>(nullable: false),
                    ApplicableToMonthlyFee = table.Column<bool>(nullable: false),
                    AppliedAmount = table.Column<int>(nullable: false),
                    LastApplied = table.Column<DateTime>(nullable: true),
                    TimesApplied = table.Column<int>(nullable: false),
                    Created = table.Column<DateTime>(nullable: false),
                    ObjectId = table.Column<Guid>(nullable: true),
                    ObjectType = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountCredits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountCredits_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgreementAffirmations",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: false),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    IPAddress = table.Column<string>(nullable: true),
                    UserAgent = table.Column<string>(nullable: true),
                    Browser = table.Column<string>(nullable: true),
                    Device = table.Column<string>(nullable: true),
                    ProvidedName = table.Column<string>(nullable: true),
                    AgreementName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgreementAffirmations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgreementAffirmations_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Charges",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: false),
                    InvoiceId = table.Column<string>(nullable: true),
                    Amount = table.Column<int>(nullable: false),
                    Description = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    InternalObjectId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Charges_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DeviceIntakes",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: false),
                    FrontImageUrl = table.Column<string>(nullable: true),
                    BackImageUrl = table.Column<string>(nullable: true),
                    OnImageUrl = table.Column<string>(nullable: true),
                    Make = table.Column<string>(nullable: false),
                    Model = table.Column<string>(nullable: false),
                    IMEI = table.Column<string>(nullable: false),
                    Repaired = table.Column<bool>(nullable: false),
                    Balance = table.Column<bool>(nullable: false),
                    Charges = table.Column<bool>(nullable: false),
                    OriginalOwner = table.Column<bool>(nullable: false),
                    Requested = table.Column<DateTime>(nullable: false),
                    Received = table.Column<DateTime>(nullable: true),
                    Approved = table.Column<DateTime>(nullable: true),
                    IntakeType = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceIntakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceIntakes_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Identities",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: true),
                    Names = table.Column<string>(nullable: true),
                    Emails = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Identities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Identities_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IdentityDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: true),
                    DocumentUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IdentityDocuments_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LossClaims",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    DateReported = table.Column<DateTime>(nullable: false),
                    ApprovedOn = table.Column<DateTime>(nullable: true),
                    ApprovedBy = table.Column<string>(nullable: true),
                    ProcessedOn = table.Column<DateTime>(nullable: true),
                    ProfileId = table.Column<Guid>(nullable: false),
                    LossType = table.Column<int>(nullable: false),
                    UserDeviceId = table.Column<Guid>(nullable: false),
                    StockedDeviceId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LossClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LossClaims_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LossClaims_StockedDevice_StockedDeviceId",
                        column: x => x.StockedDeviceId,
                        principalTable: "StockedDevice",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LossClaims_UserDevices_UserDeviceId",
                        column: x => x.UserDeviceId,
                        principalTable: "UserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: false),
                    AccountNumber = table.Column<string>(nullable: true),
                    RoutingNumber = table.Column<string>(nullable: true),
                    AccountName = table.Column<string>(nullable: true),
                    Bank = table.Column<string>(nullable: true),
                    MaskedAccountNumber = table.Column<string>(nullable: true),
                    Active = table.Column<bool>(nullable: false),
                    Confirmed = table.Column<bool>(nullable: false),
                    Archived = table.Column<bool>(nullable: false),
                    ExternalReferenceNumber = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    LastUpdated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayAccounts_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserGivenName = table.Column<string>(nullable: true),
                    LastModified = table.Column<DateTime>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: false),
                    DeviceId = table.Column<Guid>(nullable: true),
                    UserDeviceId = table.Column<Guid>(nullable: true),
                    ExternalSubscriptionId = table.Column<string>(nullable: true),
                    ExternalSubscriptionStartDate = table.Column<DateTime>(nullable: false),
                    Initiation = table.Column<int>(nullable: false),
                    Monthly = table.Column<int>(nullable: false),
                    PlanTypeId = table.Column<Guid>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    PromoApplied = table.Column<string>(nullable: true),
                    AgreementSigned = table.Column<bool>(nullable: false),
                    MDN = table.Column<string>(nullable: true),
                    AssignedSIMICC = table.Column<string>(nullable: true),
                    ExternalWirelessPlanId = table.Column<string>(nullable: true),
                    WirelessStatus = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plans_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Plans_PlanTypes_PlanTypeId",
                        column: x => x.PlanTypeId,
                        principalTable: "PlanTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Plans_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Plans_UserDevices_UserDeviceId",
                        column: x => x.UserDeviceId,
                        principalTable: "UserDevices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProfileCreditCardAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: false),
                    CreditCardNumber = table.Column<string>(nullable: true),
                    ExternalId = table.Column<string>(nullable: true),
                    Active = table.Column<bool>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    LastUpdated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileCreditCardAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileCreditCardAccounts_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: false),
                    SettingName = table.Column<string>(nullable: true),
                    SettingValue = table.Column<string>(nullable: true),
                    DateAdded = table.Column<DateTime>(nullable: false),
                    ExpirationDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileSettings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileSettings_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Line1 = table.Column<string>(nullable: true),
                    Line2 = table.Column<string>(nullable: true),
                    City = table.Column<string>(nullable: true),
                    Provence = table.Column<string>(nullable: true),
                    PostalCode = table.Column<string>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: false),
                    LastUpdated = table.Column<DateTime>(nullable: false),
                    Source = table.Column<int>(nullable: false),
                    IdentityId = table.Column<Guid>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Identities_IdentityId",
                        column: x => x.IdentityId,
                        principalTable: "Identities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LossClaimUploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    LossClaimId = table.Column<Guid>(nullable: false),
                    ImageUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LossClaimUploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LossClaimUploads_LossClaims_LossClaimId",
                        column: x => x.LossClaimId,
                        principalTable: "LossClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ACHAuthorizationAgreements",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ProfileId = table.Column<Guid>(nullable: false),
                    PlanId = table.Column<Guid>(nullable: true),
                    Timestamp = table.Column<DateTime>(nullable: false),
                    IPAddress = table.Column<string>(nullable: true),
                    UserAgent = table.Column<string>(nullable: true),
                    Browser = table.Column<string>(nullable: true),
                    Device = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACHAuthorizationAgreements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ACHAuthorizationAgreements_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ACHAuthorizationAgreements_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PortRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    PlanId = table.Column<Guid>(nullable: true),
                    MDN = table.Column<string>(nullable: true),
                    AccountNumber = table.Column<string>(nullable: true),
                    Password = table.Column<string>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    CanBeSubmitted = table.Column<bool>(nullable: false),
                    LastUpdate = table.Column<DateTime>(nullable: false),
                    DateSubmitted = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PortRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PortRequests_Plans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountCredits_ProfileId",
                table: "AccountCredits",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ACHAuthorizationAgreements_PlanId",
                table: "ACHAuthorizationAgreements",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ACHAuthorizationAgreements_ProfileId",
                table: "ACHAuthorizationAgreements",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_IdentityId",
                table: "Addresses",
                column: "IdentityId");

            migrationBuilder.CreateIndex(
                name: "IX_AgreementAffirmations_ProfileId",
                table: "AgreementAffirmations",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Charges_ProfileId",
                table: "Charges",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceIntakes_ProfileId",
                table: "DeviceIntakes",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceOptions_DeviceId",
                table: "DeviceOptions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceOptions_StockedDeviceId",
                table: "DeviceOptions",
                column: "StockedDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DiscountCodes_PlanTypeId",
                table: "DiscountCodes",
                column: "PlanTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Identities_ProfileId",
                table: "Identities",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_IdentityDocuments_ProfileId",
                table: "IdentityDocuments",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LossClaims_ProfileId",
                table: "LossClaims",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LossClaims_StockedDeviceId",
                table: "LossClaims",
                column: "StockedDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_LossClaims_UserDeviceId",
                table: "LossClaims",
                column: "UserDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_LossClaimUploads_LossClaimId",
                table: "LossClaimUploads",
                column: "LossClaimId");

            migrationBuilder.CreateIndex(
                name: "IX_PayAccounts_ProfileId",
                table: "PayAccounts",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_DeviceId",
                table: "Plans",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_PlanTypeId",
                table: "Plans",
                column: "PlanTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_ProfileId",
                table: "Plans",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_UserDeviceId",
                table: "Plans",
                column: "UserDeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_PlanTypeDevices_DeviceId",
                table: "PlanTypeDevices",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_PortRequests_PlanId",
                table: "PortRequests",
                column: "PlanId",
                unique: true,
                filter: "[PlanId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileCreditCardAccounts_ProfileId",
                table: "ProfileCreditCardAccounts",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_AccountId",
                table: "Profiles",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_BillingAddressId",
                table: "Profiles",
                column: "BillingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_ShippingAddressId",
                table: "Profiles",
                column: "ShippingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileSettings_ProfileId",
                table: "ProfileSettings",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_StockedDevice_DeviceId",
                table: "StockedDevice",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_UserDevices_StockedDeviceId",
                table: "UserDevices",
                column: "StockedDeviceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_Addresses_BillingAddressId",
                table: "Profiles",
                column: "BillingAddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_Addresses_ShippingAddressId",
                table: "Profiles",
                column: "ShippingAddressId",
                principalTable: "Addresses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Identities_Profiles_ProfileId",
                table: "Identities");

            migrationBuilder.DropTable(
                name: "AccountCredits");

            migrationBuilder.DropTable(
                name: "ACHAuthorizationAgreements");

            migrationBuilder.DropTable(
                name: "AgreementAffirmations");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Charges");

            migrationBuilder.DropTable(
                name: "DeviceIntakes");

            migrationBuilder.DropTable(
                name: "DeviceOptions");

            migrationBuilder.DropTable(
                name: "DiscountCodes");

            migrationBuilder.DropTable(
                name: "IdentityDocuments");

            migrationBuilder.DropTable(
                name: "InviteCodes");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "LossClaimUploads");

            migrationBuilder.DropTable(
                name: "PayAccounts");

            migrationBuilder.DropTable(
                name: "PlanTypeDevices");

            migrationBuilder.DropTable(
                name: "PortRequests");

            migrationBuilder.DropTable(
                name: "ProfileCreditCardAccounts");

            migrationBuilder.DropTable(
                name: "ProfileSettings");

            migrationBuilder.DropTable(
                name: "Settings");

            migrationBuilder.DropTable(
                name: "StockedSims");

            migrationBuilder.DropTable(
                name: "UserMessages");

            migrationBuilder.DropTable(
                name: "Verbiage");

            migrationBuilder.DropTable(
                name: "WebHookObjects");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "LossClaims");

            migrationBuilder.DropTable(
                name: "Plans");

            migrationBuilder.DropTable(
                name: "PlanTypes");

            migrationBuilder.DropTable(
                name: "UserDevices");

            migrationBuilder.DropTable(
                name: "StockedDevice");

            migrationBuilder.DropTable(
                name: "Devices");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "Identities");
        }
    }
}
