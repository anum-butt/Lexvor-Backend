using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lexvor.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Lexvor.API.Objects;
using Lexvor.API.Objects.User;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Lexvor.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser> {
        public DbSet<Profile> Profiles { get; set; }
        public DbSet<Device> Devices { get; set; }
        public DbSet<UserPlan> Plans { get; set; }
        public DbSet<PlanType> PlanTypes { get; set; }
		public DbSet<LinePricing> LinePricing { get; set; }
		public DbSet<AccountCredit> AccountCredits { get; set; }
        public DbSet<DiscountCode> DiscountCodes { get; set; }
	    public DbSet<UserDevice> UserDevices { get; set; }
		public DbSet<Charge> Charges { get; set; }
        public DbSet<BankAccount> PayAccounts { get; set; }
        public DbSet<Address> Addresses { get; set; }
        public DbSet<PlanTypeDevice> PlanTypeDevices { get; set; }
        public DbSet<ProfileCreditCardAccount> ProfileCreditCardAccounts { get; set; }
        public DbSet<ProfileSetting> ProfileSettings { get; set; }
        public DbSet<StockedSim> StockedSims { get; set; }
        public DbSet<StockedDevice> StockedDevice { get; set; }
        public DbSet<DeviceIntake> DeviceIntakes { get; set; }
        public DbSet<IdentityDocument> IdentityDocuments { get; set; }
        public DbSet<Identity> Identities { get; set; }
        public DbSet<DeviceOption> DeviceOptions { get; set; }
        public DbSet<PortRequest> PortRequests { get; set; }
        public DbSet<UsageDay> UsageDays { get; set; }
		public DbSet<UserComm> UserComms { get; set; }
		public DbSet<PlanAccessory> Accessories { get; set; }
		public DbSet<UserOrder> UserOrders { get; set; }
		public DbSet<UserAccessory> UserAccessories { get; set; }
		public DbSet<JobStatus> JobStatus { get; set; }

		// System Objects
		public DbSet<LoginAttempt> LoginAttempts { get; set; }
        public DbSet<AgreementAffirmation> AgreementAffirmations { get; set; }
        public DbSet<ACHAuthorizationAgreement> ACHAuthorizationAgreements { get; set; }
        public DbSet<Verbiage> Verbiage { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<WebhookResponseObject> WebhooksResponses { get; set; }
        public DbSet<EmailLog> EmailLogs { get; set; }

	    public DbSet<InviteCode> InviteCodes { get; set; }
	    public DbSet<WebHookObject> WebHookObjects { get; set; }
	    public DbSet<UserMessage> UserMessages { get; set; }
        public DbSet<LossClaim> LossClaims { get; set; }
        public DbSet<LossClaimUpload> LossClaimUploads { get; set; }
        public DbSet<UserDocuments> UserDocuments { get; set; }


		public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

	    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
		    optionsBuilder.EnableSensitiveDataLogging();
			base.OnConfiguring(optionsBuilder);
	    }

	    protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<PlanTypeDevice>()
                .HasKey(t => new { t.PlanTypeId, t.DeviceId });

            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);
        }
		
        public static void Seed(IApplicationBuilder applicationBuilder)
        {
            var scopeFactory = applicationBuilder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var roles = new List<string>();

                Type type = typeof(Roles);
                foreach (var p in type.GetProperties()) {
                    roles.Add(p.GetValue(null, null).ToString());
                }

                foreach (string role in roles)
                {
                    if (!context.Roles.Any(r => r.Name == role))
                    {
                        context.Roles.Add(new IdentityRole(role) { NormalizedName = role.ToUpper() });
                    }
                }

                var user = new ApplicationUser
                {
                    Email = "cory@westroppstudios.com",
                    NormalizedEmail = "CORY@WESTROPPSTUDIOS.COM",
                    UserName = "cory@westroppstudios.com",
                    NormalizedUserName = "CORY@WESTROPPSTUDIOS.COM",
                    EmailConfirmed = true,
                    PhoneNumberConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString("D")
                };


                if (!context.Users.Any(u => u.UserName == user.UserName))
                {
                    var password = new PasswordHasher<ApplicationUser>();
                    var hashed = password.HashPassword(user, "Y6qo8zYdbOrxZOA5");
                    user.PasswordHash = hashed;

                    context.Users.Add(user);
                }

                context.SaveChanges();

                var result = AssignRoles(applicationBuilder, user.Email, roles.ToArray()).Result;

                context.SaveChanges();
            }
        }

        public static async Task<IdentityResult> AssignRoles(IApplicationBuilder applicationBuilder, string email, string[] roles)
        {
            var scopeFactory = applicationBuilder.ApplicationServices.GetRequiredService<IServiceScopeFactory>();
            using (var scope = scopeFactory.CreateScope())
            {
                UserManager<ApplicationUser> _userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                ApplicationUser user = await _userManager.FindByEmailAsync(email);
                var result = await _userManager.AddToRolesAsync(user, roles);
						
                return result;
            }
        }
    }
}
