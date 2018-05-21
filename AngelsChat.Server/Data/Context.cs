using AngelsChat.Server.Data.Entities;
using AngelsChat.Server.Settings;
using System.Data.Entity;

namespace AngelsChat.Server.Data
{
    [DbConfigurationType(typeof(PostgresqlConfiguration))]
    public partial class Context : DbContext
    {
        #region connection string
        const string Server = "127.0.0.1";
        const int Port = 5432;
        const string Db = "AngelsChat";
        const string Login = "postgres";
        const string Password = "postgres";

        static string ConnectionString =
            $"Server={Server};" +
            $"Port={Port};" +
            $"Database={Db};" +
            $"User Id={Login};" +
            $"Password={Password};";
        #endregion

        public Context(EfSettings ef) : base(ConnectionString) { }
        public Context() : base("ChatConnectionString") { }

        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<FileMessage> FileMessages { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Video> Videos { get; set; }
        public DbSet<VideoElement> VideoElements { get; set; }
        public DbSet<Room> Rooms { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasKey(_ => _.Name);

            modelBuilder.Entity<Image>()
                .HasRequired(c => c.User)
                .WithOptional(c => c.Image);

            modelBuilder.Entity<Room>()
                .HasMany(p => p.Users)
                .WithMany(c => c.Rooms)
                .Map(m =>
                {
                    m.ToTable("UserRooms");
                    m.MapLeftKey("RoomId");
                    m.MapRightKey("UserId");
                });

            modelBuilder.Entity<User>()
                .HasMany(p => p.Messages)
                .WithRequired(p => p.User);

            modelBuilder.Entity<FileMessage>()
                .HasOptional(_ => _.Content)
                .WithRequired(_ => _.Message);

            modelBuilder.Entity<User>()
                .HasMany(p => p.Videos)
                .WithRequired(p => p.User);

            modelBuilder.Entity<Video>()
                .HasMany(p => p.VideoList)
                .WithRequired(p => p.Video);

            modelBuilder.Entity<Room>()
                .HasMany(p => p.Messages)
                .WithRequired(p => p.Room);

            //modelBuilder.Entity<User>()
            //    .HasKey(_ => _.Name);

            ////modelBuilder.Entity<Message>()
            ////    .HasRequired(_ => _.User)
            ////    .WithMany(_ => _.Messages)
            ////    .HasForeignKey(_ => _.UserName);

            ////modelBuilder.Entity<Message>()
            ////    .HasRequired(_ => _.Room)
            ////    .WithMany(_ => _.Messages)
            ////    .HasForeignKey(_ => _.RoomId);

            //modelBuilder.Entity<Room>()
            //    .HasRequired(_ => _.Owner);

            ////modelBuilder.Entity<Room>()
            ////    .HasRequired(_ => _.User)
            ////    .WithMany(_ => _.Rooms)
            ////    .HasForeignKey(_ => _.UserName);

            //modelBuilder.Entity<Room>().HasMany(_ => _.Users).WithMany(_ => _.Rooms);

            //modelBuilder.Entity<User>().HasOptional(_ => _.Image).WithRequired(_ => _.User);
            //modelBuilder.Entity<Image>().HasKey(_ => _.Id);

            ////modelBuilder.Entity<Message>()
            ////    .Property(m => m.Date)
            ////    .HasColumnType("datetime2");

            //modelBuilder.Entity<FileMessage>()
            //    .HasOptional(_ => _.Content)
            //    .WithRequired(_ => _.Message);

            //modelBuilder.Entity<Video>()
            //    .HasRequired(_ => _.User);

            base.OnModelCreating(modelBuilder);
        }
    }
}
