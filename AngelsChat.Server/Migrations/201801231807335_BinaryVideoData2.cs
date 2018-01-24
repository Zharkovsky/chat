namespace AngelsChat.Server.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class BinaryVideoData2 : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.VideoElements", "InternalData", c => c.String());
            DropColumn("dbo.VideoElements", "Data");
        }
        
        public override void Down()
        {
            AddColumn("dbo.VideoElements", "Data", c => c.Binary(maxLength: 16, fixedLength: true));
            DropColumn("dbo.VideoElements", "InternalData");
        }
    }
}
