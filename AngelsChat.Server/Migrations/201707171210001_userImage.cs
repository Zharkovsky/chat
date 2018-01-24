namespace AngelsChat.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class userImage : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Images",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Picture = c.Binary(),
                        User_Name = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.User_Name)
                .Index(t => t.User_Name);
            
            AddColumn("dbo.Users", "ImageId", c => c.Int());
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Images", "User_Name", "dbo.Users");
            DropIndex("dbo.Images", new[] { "User_Name" });
            DropColumn("dbo.Users", "ImageId");
            DropTable("dbo.Images");
        }
    }
}
