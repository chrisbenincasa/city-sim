using Godot;
using System;

namespace Borough.Shell;

public partial class ExpandedStudy
{
    private void Neighbourhood()
    {
        void Asset(string name,float x,float z,string palette="warm-slate",float yaw=0,bool anchor=false,float brick=1)
        {
            _brickScale=brick;AddAsset(name,new Vector3(x,0,z),yaw,palette,anchor);_brickScale=1;
        }
        GroundBox("ground",new Vector3(-5,-.2f,-8),new Vector3(130,.2f,145),"82856f");
        GroundBox("cross street",new Vector3(-5,-.06f,14),new Vector3(130,.12f,12),"565a58");
        GroundBox("main street",new Vector3(16,-.055f,-8),new Vector3(12,.11f,145),"565a58");
        Asphalt(new Vector3(-5,.001f,14),new Vector2(130,12));
        Asphalt(new Vector3(16,.002f,-8),new Vector2(12,145));
        GroundBox("north pavement",new Vector3(-13,-.08f,-21),new Vector3(46,.16f,58),"aaa99e");
        GroundBox("south pavement",new Vector3(-13,-.08f,34),new Vector3(46,.16f,28),"aaa99e");
        GroundBox("east pavement",new Vector3(29,-.08f,-8),new Vector3(14,.16f,145),"aaa99e");
        for(int i=0;i<46;i++)
        {
            float x=-35.5f+i;
            GroundBox("north kerb",new Vector3(x,.055f,7.8f),new Vector3(.985f,.11f,.3f),"c0beb0");
            GroundBox("south kerb",new Vector3(x,.055f,20.2f),new Vector3(.985f,.11f,.3f),"c0beb0");
        }
        for(int i=0;i<105;i++)
        {
            float z=-60+i;
            if(z>7&&z<21)continue;
            foreach(float x in new[]{9.8f,22.2f})GroundBox("main road kerb",new Vector3(x,.055f,z),new Vector3(.3f,.11f,.985f),"c0beb0");
        }
        // Surface joints are shallow geometry, at the sidewalk rather than the carriageway.
        for(int i=0;i<45;i++)
        {
            float x=-35+i;
            GroundBox("paving joint",new Vector3(x,.004f,4.7f),new Vector3(.013f,.008f,5.7f),"8d8e84");
        }
        for(int i=0;i<6;i++)GroundBox("paving joint",new Vector3(-13,.004f,2+i),new Vector3(46,.008f,.013f),"8d8e84");
        for(int i=0;i<15;i++)
        {
            float x=-55+i*7;
            if(x>7&&x<24)continue;
            GroundBox("centre dash",new Vector3(x,.008f,14),new Vector3(3,.016f,.12f),"d1cfae");
        }
        foreach(float z in new[]{5.9f,22.1f})for(int i=0;i<7;i++)GroundBox("crosswalk",new Vector3(11.4f+i*1.5f,.008f,z),new Vector3(.7f,.016f,2),"d5d3c6");
        foreach(float x in new[]{7.8f,24.2f})for(int i=0;i<7;i++)GroundBox("crosswalk",new Vector3(x,.009f,9.4f+i*1.5f),new Vector3(2,.018f,.7f),"d5d3c6");
        Asset("corner-shop",-1,-7,"earth-terracotta",brick:1.1f);
        Asset("attached-townhouse",-18,-6,brick:.85f);
        Asset("attached-townhouse",-18,36,"earth-terracotta",180,brick:1.1f);
        Asset("residential-tower",-18,-36,brick:1);
        foreach(var p in new[]{new Vector2(-31,4),new Vector2(-9,4),new Vector2(6,-18),new Vector2(6,-36),new Vector2(-31,25),new Vector2(27,-7),new Vector2(27,29)})
        {
            GroundBox("tree pit",new Vector3(p.X,.005f,p.Y),new Vector3(1.6f,.01f,1.6f),"575746");
            Asset("street-tree",p.X,p.Y);
        }
        Asset("car-realistic",-24,10,"warm-slate",90,true);
        Asset("car-realistic",-8,18,"earth-terracotta",-90,true);
        Asset("car-realistic",19,-14,"original",0,true);
        Asset("car-realistic",13,33,"warm-slate",180,true);
        foreach(var p in new[]{new Vector2(4,3),new Vector2(-6,5),new Vector2(-22,4),new Vector2(-27,24),new Vector2(25,-5),new Vector2(-11,-23)})Asset("walker",p.X,p.Y,"original",p.X*11);
        foreach(var p in new[]{new Vector2(-29,6),new Vector2(8,-18),new Vector2(24,27),new Vector2(-5,22)})
        {
            GroundBox("lamp base",new Vector3(p.X,.15f,p.Y),new Vector3(.3f,.3f,.3f),"434b47");
            Cylinder("lamp post",new Vector3(p.X,3,p.Y),.055f,6,"434b47");
            GroundBox("lamp arm",new Vector3(p.X+.55f,5.92f,p.Y),new Vector3(1.2f,.075f,.075f),"434b47");
            GroundBox("lamp housing",new Vector3(p.X+1.1f,5.87f,p.Y),new Vector3(.6f,.1f,.25f),"434b47");
            GroundBox("lamp lens",new Vector3(p.X+1.1f,5.81f,p.Y),new Vector3(.48f,.025f,.18f),"d8d4b9");
        }
        foreach(var p in new[]{new Vector2(-26,3),new Vector2(5,-24)})
        {
            for(int j=0;j<5;j++)GroundBox("bench seat slat",new Vector3(p.X,.47f,p.Y+j*.09f),new Vector3(1.7f,.045f,.075f),"84725a");
            for(int j=0;j<3;j++)GroundBox("bench back slat",new Vector3(p.X,.66f+j*.11f,p.Y+.43f),new Vector3(1.7f,.08f,.04f),"84725a");
            foreach(float dx in new[]{-.65f,.65f})GroundBox("bench leg",new Vector3(p.X+dx,.23f,p.Y+.2f),new Vector3(.05f,.46f,.45f),"424a45");
            Cylinder("litter bin",new Vector3(p.X+1.3f,.4f,p.Y),.22f,.8f,"49574b");
        }
        foreach(var p in new[]{new Vector2(3,-23),new Vector2(-32,-22),new Vector2(-32,-42)})
        {
            GroundBox("garden bed",new Vector3(p.X,.04f,p.Y),new Vector3(3,.08f,5),"687356");
            for(int i=0;i<4;i++)GroundBox("garden stepping stone",new Vector3(p.X,.09f,p.Y-1.8f+i*1.1f),new Vector3(.55f,.1f,.65f),"aaa798");
        }
    }

    private void Cylinder(string name,Vector3 position,float radius,float height,string color)
    {
        _content.AddChild(new MeshInstance3D { Name=name,Position=position,Mesh=new CylinderMesh { TopRadius=radius,BottomRadius=radius,Height=height,RadialSegments=16 },MaterialOverride=new StandardMaterial3D { AlbedoColor=new Color(color),Roughness=.7f } });
    }

    private void Asphalt(Vector3 position,Vector2 size)
    {
        const string prefix="res://assets/visual-study/neighbourhood/materials/Asphalt012_1K-JPG_";
        var material=new StandardMaterial3D
        {
            AlbedoTexture=GD.Load<Texture2D>(prefix+"Color.jpg"),
            NormalEnabled=true,NormalTexture=GD.Load<Texture2D>(prefix+"NormalGL.jpg"),NormalScale=.35f,
            RoughnessTexture=GD.Load<Texture2D>(prefix+"Roughness.jpg"),RoughnessTextureChannel=BaseMaterial3D.TextureChannel.Red,
            Uv1Scale=new Vector3(size.X/4,size.Y/4,1),
            Uv1Offset=new Vector3(position.X/4-size.X/8,position.Z/4-size.Y/8,0)
        };
        if(material.AlbedoTexture==null||material.NormalTexture==null||material.RoughnessTexture==null)throw new InvalidOperationException("Asphalt maps missing");
        _content.AddChild(new MeshInstance3D { Name="asphalt wearing surface",Position=position,Mesh=new PlaneMesh { Size=size },MaterialOverride=material });
    }
}
