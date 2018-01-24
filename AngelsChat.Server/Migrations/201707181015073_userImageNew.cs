namespace AngelsChat.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class userImageNew : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Images", "Width", c => c.Int(nullable: false));
            AddColumn("dbo.Images", "Height", c => c.Int(nullable: false));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Images", "Height");
            DropColumn("dbo.Images", "Width");
        }
    }
}
