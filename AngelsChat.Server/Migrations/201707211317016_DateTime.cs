namespace AngelsChat.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class DateTime : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.Users", "Date", c => c.DateTime(nullable: false));
            AddColumn("dbo.Messages", "Date", c => c.DateTime(nullable: false, precision: 7, storeType: "datetime2"));
        }
        
        public override void Down()
        {
            DropColumn("dbo.Messages", "Date");
            DropColumn("dbo.Users", "Date");
        }
    }
}
