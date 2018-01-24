namespace AngelsChat.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BinaryVideoData3 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.VideoElements", "Data", c => c.Binary());
        }
        
        public override void Down()
        {
            DropColumn("dbo.VideoElements", "Data");
        }
    }
}
