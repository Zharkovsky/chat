using AngelsChat.Server.Data.Entities;
using AngelsChat.Server.Settings;
using System.Data.Entity;

namespace AngelsChat.Server.Data
{
    public partial class Context : DbContext
    {
        public Context(EfSettings ef) : base($"data source={ef.Source};Initial Catalog={ef.Name};Integrated Security=True;") { }
        public Context() : base("ChatConnectionString") { }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<FileMessage> FileMessages { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<VideoElement> VideoElements { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(_ => _.Name);

            modelBuilder.Entity<Message>()
                .HasRequired(_ => _.User)
                .WithMany(_ => _.Messages)
                .HasForeignKey(_ => _.UserName);

            modelBuilder.Entity<User>().HasOptional(_ => _.Image).WithRequired(_ => _.User);
            modelBuilder.Entity<Image>().HasKey(_ => _.Id);

            modelBuilder.Entity<Message>()
                .Property(m => m.Date)
                .HasColumnType("datetime2");

            modelBuilder.Entity<FileMessage>()
                .HasOptional(_ => _.Content)
                .WithRequired(_ => _.Message);

            modelBuilder.Entity<Video>()
                .HasRequired(_ => _.User);

            base.OnModelCreating(modelBuilder);
        }
    }
}
