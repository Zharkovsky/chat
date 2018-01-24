namespace AngelsChat.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class fileMessage : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.BinaryContents",
                c => new
                    {
                        Id = c.Int(nullable: false),
                        Content = c.Binary(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Messages", t => t.Id)
                .Index(t => t.Id);
            
            AddColumn("dbo.Messages", "FileName", c => c.String());
            AddColumn("dbo.Messages", "FileWeight", c => c.Long());
            AddColumn("dbo.Messages", "Discriminator", c => c.String(nullable: false, maxLength: 128));
            AddColumn("dbo.Messages", "User_Name", c => c.String(maxLength: 128));
            CreateIndex("dbo.Messages", "User_Name");
            AddForeignKey("dbo.Messages", "User_Name", "dbo.Users", "Name");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.Messages", "User_Name", "dbo.Users");
            DropForeignKey("dbo.BinaryContents", "Id", "dbo.Messages");
            DropIndex("dbo.BinaryContents", new[] { "Id" });
            DropIndex("dbo.Messages", new[] { "User_Name" });
            DropColumn("dbo.Messages", "User_Name");
            DropColumn("dbo.Messages", "Discriminator");
            DropColumn("dbo.Messages", "FileWeight");
            DropColumn("dbo.Messages", "FileName");
            DropTable("dbo.BinaryContents");
        }
    }
}
