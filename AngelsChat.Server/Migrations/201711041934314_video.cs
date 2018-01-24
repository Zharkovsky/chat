namespace AngelsChat.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class video : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Videos",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        User_Name = c.String(nullable: false, maxLength: 128),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Users", t => t.User_Name, cascadeDelete: true)
                .Index(t => t.User_Name);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Videos", "User_Name", "dbo.Users");
            DropIndex("dbo.Videos", new[] { "User_Name" });
            DropTable("dbo.Videos");
        }
    }
}
