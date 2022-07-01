using System;
using System.Linq;
using System.IO;
using System.Text;
using MDXLib.MDX;
using M2Lib.m2;
using M2Lib.types;

namespace Alpha_m2_Converter
{
    class Program
    {

        static void Main(string[] args)
        {
            if (args.Length == 0)
                Console.WriteLine("This a drag n drop tool. To use, simply drag your 0.5.3 MDX files on this executable, they will be exported to this folder.\n");
            else
            { // info
                Console.WriteLine("Not implemented yet : Particles emitters, ribbon emitters,  texture animations, lights");
            }

            List<string> Filepaths = new List<string>();
            Filepaths.AddRange(args);
            // hardcode loading models for developement
            // Filepaths.Add(@"D:\titi tools\alpha m2 converter\Alpha m2 Converter\Alpha m2 Converter\bin\Debug\net6.0\Murloc.mdx");
            // Filepaths.Add(@"D:\titi tools\alpha m2 converter\Alpha m2 Converter\Alpha m2 Converter\bin\Debug\net6.0\Ballista.mdx");

            // var testm2 = new M2();
            // testm2.Load(new BinaryReader(File.Open(@"F:\WoWModding\aExtractedClients\WOTLK ClientFiles\World\AZEROTH\ELWYNN\PASSIVEDOODADS\BALLISTA\Ballista.m2", FileMode.Open), Encoding.UTF8, false), M2.Format.LichKing);

            foreach (string fileName in Filepaths)
            {
                if (fileName.Length > 0 && File.Exists(fileName))
                {
                    Console.WriteLine("Loading " + fileName + "...");

                    ////////// load mdx model data ////////////////
                    Model mdx = new Model(fileName);

                    VERS version = mdx.Get<VERS>();
                    MODL modl = mdx.Get<MODL>();
                    SEQS sequences = mdx.Get<SEQS>();
                    MTLS materials = mdx.Get<MTLS>();
                    TEXS textures = mdx.Get<TEXS>();
                    GEOS geosets = mdx.Get<GEOS>();
                    GEOA geosetanims = mdx.Get<GEOA>();
                    HELP helpers = mdx.Get<HELP>();
                    ATCH attachments = mdx.Get<ATCH>();
                    PIVT pivotpoints = mdx.Get<PIVT>();
                    CAMS cameras = mdx.Get<CAMS>();
                    EVTS events = mdx.Get<EVTS>();
                    HTST hittestshapes = mdx.Get<HTST>();
                    CLID collisions = mdx.Get<CLID>();
                    GLBS globalsequences = mdx.Get<GLBS>();
                    PRE2 particleemitter2s = mdx.Get<PRE2>();
                    RIBB ribbonemitters = mdx.Get<RIBB>();
                    LITE lights = mdx.Get<LITE>();
                    TXAN textureanimations = mdx.Get<TXAN>();
                    BONE bones = mdx.Get<BONE>();

                    //////// initialise  M2 ///////////////
                    M2 M2Ouput = new M2();

                    M2Ouput.Version = M2.Format.LichKing;
                    M2Ouput.Name = modl.Name;
                    M2Ouput.BoundingBox = modl.Bounds.Extent.ToCAaBox;
                    M2Ouput.BoundingSphereRadius = modl.Bounds.Radius;
                    // collision box ?

                    // maybe global flags ?
                    // M2Ouput.GlobalSequences.AddRange(globalsequences); // global sequences/loops
                    if (globalsequences != null)
                        foreach (var mdxglobalsequence in globalsequences)
                        {
                            // Console.WriteLine(mdxglobalsequence);
                            M2Ouput.GlobalSequences.Add(mdxglobalsequence);
                        }

                    // sequences/animations
                    // TODO : Merge animations variations ?
                    if (sequences != null)
                        foreach (Sequence mdxsequence in sequences)
                        {
                            M2Sequence newm2sequence = new M2Sequence();

                            // convert sequence name to anim id
                            string[] splitname = mdxsequence.Name.Split(" ");


                            if (ressources.AnimationDataWotlk.ContainsKey(splitname[0]))
                            {
                                // some non existing anim names in dbc : "Morph", 

                                int animId = ressources.AnimationDataWotlk[splitname[0]];
                                newm2sequence.AnimationId = ((ushort)animId);
                            }
                            else
                            {
                                Console.WriteLine("Animation name " + "\"" + mdxsequence.Name + "\"" + " doesn't exist in AnimationData");
                                // continue; // skip non existing animation types
                            }

                            // 
                            // newm2sequence.SubAnimationId = ;
                            newm2sequence.Length = ((uint)(mdxsequence.MaxTime - mdxsequence.MinTime)); // wotlk only
                            newm2sequence.MovingSpeed = mdxsequence.MoveSpeed;
                            // newm2sequence.Flags = 0; // todo : only mdx flag is : 1 = non looping, findout what it is in m2. note m2lib initialises 0x20(no anim file) by default
                            if (mdxsequence.Frequency == 0)
                                mdxsequence.Frequency = 1; // mdx seems to default to 0 instead of 1.
                            newm2sequence.Probability = ((short)(mdxsequence.Frequency * 32767)); // 1.0 scale in mdx, 32767 in m2
                            newm2sequence.MinimumRepetitions = ((uint)mdxsequence.MinReplay);
                            newm2sequence.MaximumRepetitions = ((uint)mdxsequence.MaxReplay);
                            newm2sequence.BlendTimeEnd = ((ushort)mdxsequence.BlendTime); // m2lib writes like this : (BlendTimeEnd + BlendTimeStart) / 2 (wotlk only has 1 blend time var)
                            newm2sequence.BlendTimeStart = ((ushort)mdxsequence.BlendTime);
                            newm2sequence.Bounds = modl.Bounds.Extent.ToCAaBox; // just using base model bounds
                            newm2sequence.BoundRadius = modl.Bounds.Radius;
                            // NextAnimation
                            // AliasNext

                            M2Ouput.Sequences.Add(newm2sequence);
                        }
                    else
                        Console.WriteLine("Model has no animation !");

                    // bones
                    if (bones != null)
                    {
                        // make an uppercase list to ignore case
                        List<string> knownbonenames = new List<string>();
                        foreach (string knownbonename in ressources.KnownMdxBoneNames)
                        {
                            knownbonenames.Add(knownbonename.ToUpper());
                        }

                        foreach (var mdxbone in bones)
                        {
                            M2Bone m2Bone = new M2Bone();

                            // Console.WriteLine(mdxbone.Name);

                            if ( !knownbonenames.Contains( mdxbone.Name.ToUpper() ) )
                                Console.WriteLine("New bone name : \"" + mdxbone.Name + "\". Add it to the list and check if it's a key bone! ");
                            // maybe Name = KeyBoneid, names are different though
                            if (ressources.MdxKeyboneNames.ContainsKey(mdxbone.Name))
                                m2Bone.KeyBoneId = (M2Bone.KeyBone)ressources.MdxKeyboneNames[mdxbone.Name];
                            else
                                m2Bone.KeyBoneId = M2Bone.KeyBone.Other; // -1 default. check if it can be set

                            // M2 lib will generate a KeyBoneLookup if KeyBoneId != -1.

                            m2Bone.Flags = (M2Bone.BoneFlags)mdxbone.Flags; // check if there's no higher flag than 0x40 in mdx bone

                            bool isRootBone = false;
                            // get bone's parent
                            if (mdxbone.ParentId != -1)
                            {
                                GenObject parentobj = mdx.Hierachy.ElementAt(mdxbone.ParentId);
                                if ( !parentobj.Flags.HasFlag(MDXLib.GENOBJECTFLAGS.GENOBJECT_MDLBONESECTION)  )
                                {
                                    // it links to a helper if not a bone.
                                    // bone is a root bone
                                    // TODO : Maybe we can use the helper data
                                    // Note : There are two types of helpers : MAIN and ROOT, ROOT usualy has MAIN as a parent.
                                    // Note that those helpers have tracks

                                    if (parentobj.Name == "Root" ) // "Root" type is defined in the helper.
                                    {
                                        if ((int)m2Bone.KeyBoneId == -1)
                                        {
                                            m2Bone.KeyBoneId = M2Bone.KeyBone.Root; // id 26
                                            m2Bone.ParentBone = -1; // can try hardcoding to 0, or find the bone that has parent id = parentobj.ParentId
                                            // Console.WriteLine(mdxbone.Name);    
                                        }
                                        else
                                            m2Bone.ParentBone = 0;

                                        isRootBone = true;
                                        Console.WriteLine(mdxbone.Name);
                                        Console.WriteLine(mdxbone.ObjectId);
                                        Console.WriteLine(m2Bone.ParentBone);
                                    }
                                    // m2Bone.ParentBone = -1;
                                }
                            }

                            if ( !isRootBone)
                                m2Bone.ParentBone = (short)mdxbone.ParentId; // check if it's properly getting ids

                            m2Bone.SubmeshId = 0; // TODO, figure out this
                            // compressdata/CRC, ?

                            // test from before I found .PopulateM2Track()
                            // m2Bone.Translation.GlobalSequence =  (short)mdxbone.TranslationKeys.GlobalSequenceId;
                            // m2Bone.Translation.InterpolationType = (M2Track<C3Vector>.InterpolationTypes)mdxbone.TranslationKeys.InterpolationType;

                            if (mdxbone.TranslationKeys != null)
                                mdxbone.TranslationKeys.PopulateM2Track(m2Bone.Translation, sequences); // check if this works and if sequence arg should be all
                            if (mdxbone.RotationKeys != null)
                                mdxbone.RotationKeys.PopulateM2Track(m2Bone.Rotation, sequences);
                            if (mdxbone.ScaleKeys != null)
                                mdxbone.ScaleKeys.PopulateM2Track(m2Bone.Scale, sequences);

                            if (mdxbone.ObjectId != -1)
                                m2Bone.Pivot = pivotpoints.ElementAt(mdxbone.ObjectId).ToC3Vector;
                            // m2Bone.Pivot = new C3Vector(0.0f, 0.0f, 0.0f); // undefined in alpha ? leave default. TODO : check if default is correctly 0

                            M2Ouput.BoneLookup.Add(((short)M2Ouput.Bones.Count())); // TODO: Figure this out and what -1 is for.
                            M2Ouput.Bones.Add(m2Bone);
                        }                    
                    }
                    else
                        Console.WriteLine("Model has no bones !");

                    // Textures
                    if (textures != null)
                        foreach (Texture mdxTexture in textures)
                        {
                            M2Texture m2Texture = new M2Texture();
                            m2Texture.Type = (M2Texture.TextureType)mdxTexture.ReplaceableId;
                            m2Texture.Flags = (M2Texture.TextureFlags)mdxTexture.Flags;
                            m2Texture.Name = mdxTexture.Image;

                            M2Ouput.TexLookup.Add(((short)M2Ouput.Textures.Count()));
                            M2Ouput.Textures.Add(m2Texture);
                        }

                    // collision box
                    float minX = 0.0f;
                    float minY = 0.0f;
                    float minZ = 0.0f;
                    float maxX = 0.0f;
                    float maxY = 0.0f;
                    float maxZ = 0.0f;

                    // Collision
                    foreach (var collisionvert in collisions.Vertices)
                    {
                        M2Ouput.CollisionVertices.Add(collisionvert.ToC3Vector);
                        // fill collision box
                        if (collisionvert.X < minX)
                            minX = collisionvert.X;
                        if (collisionvert.Y < minY)
                            minY = collisionvert.Y;
                        if (collisionvert.Z < minZ)
                            minZ = collisionvert.Z;

                        if (collisionvert.Z > maxZ)
                            maxZ = collisionvert.Z;
                        if (collisionvert.Y > maxY)
                            maxY = collisionvert.Y;
                        if (collisionvert.X > maxX)
                            maxX = collisionvert.X;
                    }

                    M2Ouput.CollisionBox = new CAaBox(new C3Vector(minX, minY, minZ), new C3Vector(maxX, maxY, maxZ));
                    M2Ouput.CollisionSphereRadius = (maxZ - minZ) / 2; // TODO, temporary solution

                    foreach (var triIndice in collisions.TriIndices)
                    {
                        M2Ouput.CollisionTriangles.Add(triIndice);
                    }
                    foreach (var collNormal in collisions.FacetNormals)
                    {
                        M2Ouput.CollisionNormals.Add(collNormal.ToC3Vector);
                    }

                    // attachments
                    if (attachments != null)
                        foreach (var mdxAttachment in attachments)
                        {
                            M2Attachment m2Attachment = new M2Attachment();

                            m2Attachment.Id = ((uint)mdxAttachment.AttachmentId);
                            if (mdxAttachment.ParentId == -1) // maybe skip instead.
                                m2Attachment.Bone = 0;
                            else
                                m2Attachment.Bone = ((uint)mdxAttachment.ParentId); // parentid or objectid, check which one.

                            m2Attachment.Position = pivotpoints.ElementAt(mdxAttachment.ObjectId).ToC3Vector;
                            // m2Attachment.AnimateAttached = ; // TODO ?

                            M2Ouput.Attachments.Add(m2Attachment);
                        }

                    // events
                    if (events != null)
                        foreach (var mdxEvent in events)
                        {
                            M2Event m2Event = new M2Event();

                            m2Event.Identifier = mdxEvent.Name;
                            m2Event.Data = 0; // TODO

                            if (mdxEvent.ParentId == -1)
                                m2Event.Bone = 0; // // seems to sometimes use -1 which is not compatible
                            // else if (mdxEvent.ParentId > (bones.Count() -1 ))
                            else if ((!mdx.Hierachy.ElementAt(mdxEvent.ParentId).Flags.HasFlag(MDXLib.GENOBJECTFLAGS.GENOBJECT_MDLBONESECTION))
                                // || mdx.Hierachy.ElementAt(mdxEvent.ParentId).GetType() == typeof( Bone) )
                                && mdx.Hierachy.ElementAt(mdxEvent.ParentId).Flags.HasFlag(MDXLib.GENOBJECTFLAGS.GENOBJECT_MDLATTACHMENTSECTION))
                                 // parent is not a bone
                            {
                                // sometimes it points to an attachment id instead of a bone
                                m2Event.Bone = ((ushort)mdx.Hierachy.ElementAt(mdxEvent.ParentId).ParentId);
                            }
                            else
                                m2Event.Bone = ((ushort)mdxEvent.ParentId); 

                            // m2Event.Unknown = ;
                            m2Event.Position = pivotpoints.ElementAt(mdxEvent.ObjectId).ToC3Vector;
                            // 
                            if (sequences != null)
                                foreach (var anim in sequences) // 1 timestamp per sequence ?
                                {
                                    M2Array<uint> timestamparray = new M2Array<uint>();
                                    // timestamparray.Add(0);
                                    m2Event.Enabled.Timestamps.Add(timestamparray);
                                }
                            
                            if (mdxEvent.EventKeys != null)
                            {
                                int seqid = 0;
                                foreach (uint timestamp in mdxEvent.EventKeys.Keys) // figure out how to use this
                                {
                                    m2Event.Enabled.Timestamps.ElementAt(seqid).Add(timestamp);
                                    seqid++;
                                }
                                if (mdxEvent.EventKeys.GlobalSequenceId != -1)
                                    if (globalsequences != null)
                                        m2Event.Enabled.GlobalSequence = (short)globalsequences.ElementAt(mdxEvent.EventKeys.GlobalSequenceId);
                            }

                            // rot/scale/trans keys are always empty ?

                            M2Ouput.Events.Add(m2Event);
                        }

                    // cameras
                    if (cameras != null)
                        foreach (var mdxCamera in cameras)
                        {
                            M2Camera m2Camera = new M2Camera();

                            if (mdxCamera.Name == "CameraPortrait")
                                m2Camera.Type = M2Camera.CameraType.CharacterInfo;
                            else if (mdxCamera.Name == "Portrait")
                                m2Camera.Type = M2Camera.CameraType.Portrait; // verify
                            else if (mdxCamera.Name == "Paperdoll")
                                m2Camera.Type = M2Camera.CameraType.UserInterface;
                            else
                                Console.WriteLine("Unknown camera type name : " + mdxCamera.Name); // m2lib defaults to userinterface

                            m2Camera.FarClip = mdxCamera.FarClip;
                            m2Camera.NearClip = mdxCamera.NearClip; 
                            if (mdxCamera.TranslationKeys != null)
                                mdxCamera.TranslationKeys.PopulateM2Track(m2Camera.Positions, sequences); // m2Camera.Positions, guessed
                            m2Camera.PositionBase = mdxCamera.Pivot.ToC3Vector; // verify. maybe mdxCamera.pivot
                            if (mdxCamera.TargetTranslationKeys != null)
                                mdxCamera.TargetTranslationKeys.PopulateM2Track(m2Camera.TargetPositions, sequences); // m2Camera.TargetPositions
                            m2Camera.TargetPositionBase = mdxCamera.TargetPosition.ToC3Vector;
                            if (mdxCamera.RotationKeys != null)
                                mdxCamera.RotationKeys.PopulateM2Track(m2Camera.Roll, sequences);
                            m2Camera.FieldOfView.Values.Add(new M2Array<C3Vector> { new C3Vector(mdxCamera.FieldOfView, 0.0f, 0.0f) } ) ;

                            // maybe it is required to initialise 1 default timestamp/keyframepair

                            M2Ouput.Cameras.Add(m2Camera);
                        }

                    M2Ouput.TexUnitLookup.Add(0);
                    M2Ouput.TexUnitLookup.Add(1);
                    M2Ouput.TexUnitLookup.Add(-1); // environment mapping

                    /////////// mdx only has 1 skin profile/view //////////////////
                    M2SkinProfile Skinprofile = new M2SkinProfile();

                    Skinprofile.Bones = 256; // Maximum number of bones per drawcall for each view. Values seen : 256, 64, 53, 21
                    // IEnumerable<M2Vertex> m2Vertices = new List<M2Vertex>();

                    // no global vertex list in MDX, they're all in each geoset
                    ushort geosetid = 0;
                    foreach (Geoset1300 mdxGeoset in geosets)
                    {
                        // skinsection/geoset

                        // test exporting only specific geosets delete later////////
                        // geosetid++;
                        // if (geosetid != 2)
                        //     continue;
                        // geosetid = 0;
                        /////////////////////////////////

                        int globalvertcount = M2Ouput.GlobalVertexList.Count();

                        M2SkinSection m2SkinSection = new M2SkinSection(); // skinsection = geoset
                        m2SkinSection.SubmeshId = (ushort)mdxGeoset.SelectionGroup; // when formatted as four digits, the first two digits map to CHARACTER_GEOSET_SECTIONS, the second two digits are an associated sub-group
                        // Console.WriteLine(m2SkinSection.SubmeshId);
                        m2SkinSection.Level = 0; // verify
                        m2SkinSection.NVertices = (ushort)mdxGeoset.NrOfVertices;
                        m2SkinSection.StartVertex = (ushort)M2Ouput.GlobalVertexList.Count();
                        m2SkinSection.StartTriangle = (ushort)Skinprofile.Triangles.Count();
                        m2SkinSection.NTriangles = (ushort)mdxGeoset.NrOfFaceVertices;
                        // m2SkinSection.NBones = (ushort)mdxGeoset.BoneIndexes.Count();
                        // m2SkinSection.NBones = ((ushort)mdxGeoset.NrOfBoneIndexes);
                        m2SkinSection.NBones = 1; // if 0, client throws divide by 0 error.
                        // get bone count either from bone.geosetid or count different bones in mdxGeoset.BoneWeights
                        foreach (var bone in bones)
                        {
                            // Console.WriteLine(bone.GeosetId);
                            if (bone.GeosetId == geosetid)
                                m2SkinSection.NBones++;
                        }

                        m2SkinSection.StartBones = (ushort)mdxGeoset.BoneIndexes[0]; // Very scuffed. probably need to reorder and set bones lookup there instead

                        m2SkinSection.BoneInfluences = 1; // TODO. Highest number of bones referenced by a vertex of this submesh. 0 to 4. Setting it too high makes the model invisible
                        m2SkinSection.RootBone = (ushort)mdxGeoset.BoneIndexes[0]; // TODO. just using first bone for now
                        m2SkinSection.CenterMass = mdxGeoset.GetCenter(); // maybe mdxGeoset.Bounds.Extent.Min
                        m2SkinSection.CenterBoundingBox = mdxGeoset.Bounds.Extent.Max.ToC3Vector;
                        m2SkinSection.Radius = mdxGeoset.Bounds.Radius;

                        // Console.WriteLine(mdxGeoset.FaceGroups.Count()); // always 1 ?
                        // foreach (var facegroup in mdxGeoset.FaceGroups)
                        //     Console.WriteLine(facegroup);

                        // set triangles in skinprofile
                        foreach (var face in mdxGeoset.FaceVertices)
                        {
                            Skinprofile.Triangles.Add((ushort)(face.Vertex1 + globalvertcount));
                            Skinprofile.Triangles.Add((ushort)(face.Vertex2 + globalvertcount));
                            Skinprofile.Triangles.Add((ushort)(face.Vertex3 + globalvertcount));
                        }

                        ushort i = 0;
                        foreach (var mdxvert in mdxGeoset.Vertices)
                        {
                            M2Vertex m2Vertex = new M2Vertex();
                            m2Vertex.Position = mdxvert.ToC3Vector;
                            
                            
                            m2Vertex.Normal = mdxGeoset.Normals[i].ToC3Vector;

                            // mdxGeoset.TexCoords[i].Y -= 1; // UV is 1.0 higher in alpha
                            mdxGeoset.TexCoords[i].Y = -mdxGeoset.TexCoords[i].Y; // invert Y axis.
                            m2Vertex.TexCoords[0] = mdxGeoset.TexCoords[i].ToC2Vector; // only 1 texcoord in alpha ?

                            // wiki says only one uint32 but it makes no sense, trying to split into 4 uint8.
                            VertexProperty vertexProperty = new VertexProperty( // try converting uint32 to 4 uint8 bytes
                                (byte)(mdxGeoset.BoneIndexes[i] & 0xFF), (byte)((mdxGeoset.BoneIndexes[i] >> 8) & 0xFF),
                                (byte)((mdxGeoset.BoneIndexes[i] >> 16) & 0xFF), (byte)((mdxGeoset.BoneIndexes[i] >> 24) & 0xFF) );

                            // seems to use a different endian than indices ? this is demonic.
                            m2Vertex.BoneWeights = new byte[4] { 
                                (byte)((mdxGeoset.BoneWeights[i] >> 24) & 0xFF),(byte)((mdxGeoset.BoneWeights[i] >> 16) & 0xFF),
                                (byte)((mdxGeoset.BoneWeights[i] >> 8) & 0xFF), (byte)(mdxGeoset.BoneWeights[i] & 0xFF)} ;

                            Skinprofile.Properties.Add(vertexProperty); // lookup table to select a subset of bones from the global bone list used by this skin. 4 indices into the global bone list

                            m2Vertex.BoneIndices = vertexProperty.Properties; // is it the same ?

                            Skinprofile.Indices.Add((ushort)(i + globalvertcount)); // add vertice id to skinprofile
                            M2Ouput.GlobalVertexList.Add(m2Vertex);
                            i++;
                        }

                        ////////// Batch/Texture unit /////////////////
                        // only one material section in alpha ? 
                        Material geosetmaterial = materials.ElementAt(((int)mdxGeoset.MaterialId));
                        // if (geosetmaterial.Layers.Count > 1)
                        //     Console.WriteLine("MDX material has more than 1 layer. This is not supported yet.");

                        
                        // Multi layer support attempt
                        ushort layerId = 0;
                        foreach (var matLayer in geosetmaterial.Layers)
                        {
                            M2Batch m2Batch = new M2Batch();

                            if (matLayer.TextureAnimationId != -1) // has animated texture
                                { m2Batch.Flags = 0; } // 16 = static(default), 0 = animated.

                            m2Batch.Flags2 = (byte)geosetmaterial.PriorityPlane;
                            m2Batch.ShaderId = 0;
                            m2Batch.SubmeshIndex = geosetid;
                            m2Batch.SubmeshIndex2 = geosetid; // seems unused
                            m2Batch.ColorIndex = -1; // doesn't exist in mdx ?


                            m2Batch.RenderFlags = ((ushort)M2Ouput.Materials.Count()); // index to materials[]
                            m2Batch.Layer = layerId;
                            m2Batch.OpCount = 1; // always 1 ?
                            // m2Batch.Texture = (ushort)mdxGeoset.MaterialId; // index to texlookuptable
                            m2Batch.Texture = ((ushort)matLayer.TextureId); // verify

                            /// TODO
                            m2Batch.TexUnitNumber2 = layerId; // can use layer id ? or 0
                            // texture unit lookup table. also used to set environ environment mapping.
                            // TODO figure out how to set -1/0/1
                            if (matLayer.Flags.HasFlag(MDXLib.MDLGEO.MODEL_GEO_SPHERE_ENV_MAP))
                            {
                                m2Batch.TexUnitNumber2 = 2; // environment mapping
                            }

                            // add material to M2 root
                            M2Material m2Material = new M2Material();
                            
                            ushort renderflags = 0;
                            if (matLayer.Flags.HasFlag(MDXLib.MDLGEO.MODEL_GEO_UNSHADED))
                                renderflags |= 0x01; // Unlit. assumning unshaded = Unlit.

                            if (matLayer.Flags.HasFlag(MDXLib.MDLGEO.MODEL_GEO_TWOSIDED))
                                renderflags |= 0x04;
                            if (matLayer.Flags.HasFlag(MDXLib.MDLGEO.MODEL_GEO_UNFOGGED))
                                renderflags |= 0x02;
                            if (matLayer.Flags.HasFlag(MDXLib.MDLGEO.MODEL_GEO_NO_DEPTH_TEST))
                                renderflags |= 0x08;
                            if (matLayer.Flags.HasFlag(MDXLib.MDLGEO.MODEL_GEO_NO_DEPTH_SET))
                                renderflags |= 0x10; // depthwrite = depthset ?

                            m2Material.Flags = (M2Material.RenderFlags)renderflags;
                            m2Material.BlendMode = (M2Material.BlendingMode)matLayer.BlendMode; // verify if blending modes match

                            M2Ouput.Materials.Add(m2Material);

                            M2TextureWeight m2TextureWeight = new M2TextureWeight();

                            
                            if (matLayer.AlphaKeys != null) // assuming layer alpha keys = m2 transparency block.
                            {
                                matLayer.AlphaKeys.PopulateM2Track(m2TextureWeight.Weight, sequences);
                                M2Ouput.TransLookup.Add((short)M2Ouput.Transparencies.Count()); // todo, make m2lib generate lookups
                                M2Ouput.Transparencies.Add(m2TextureWeight); // currently adding 1 transparancy per group, check if we can not repeat them.
                                m2Batch.Transparency = (ushort)M2Ouput.Transparencies.Count(); // set the new transparancy
                            }
                            else
                                m2Batch.Transparency = 0; // use default transparency

                            // maybe convert material static alpha to a transparancy entry
                            // var transparancy = matLayer.Alpha; // can use this ?

                            // m2Batch.TextureAnim // TODO.

                            Skinprofile.TextureUnits.Add(m2Batch);

                            layerId++;
                        }
						
                        Skinprofile.Submeshes.Add(m2SkinSection);

                        geosetid++;
                    }

                    M2Ouput.Views.Add(Skinprofile);

                    // add a default transparancy entry if none
                    if (M2Ouput.Transparencies.Count() == 0)
                    {
                        M2TextureWeight m2TextureWeight = new M2TextureWeight();
                        m2TextureWeight.Weight.Values.Add(new M2Array<FixedPoint_0_15>() { new FixedPoint_0_15(0x7FFF) } );
                        m2TextureWeight.Weight.Timestamps.Add(new M2Array<uint>() { 0 });
                        m2TextureWeight.SetSequences(M2Ouput.Sequences); // ???
                        // m2TextureWeight.Weight.Sequences[0].;

                        M2Ouput.TransLookup.Add(0);
                        M2Ouput.Transparencies.Add(m2TextureWeight); 
                    }

                    // add a default bone lookup table if none
                    if (M2Ouput.BoneLookup.Count() == 0)
                    {
                        M2Ouput.BoneLookup.Add(0);
                    }

                    // need an empty tex anim lookup ?
                    if (M2Ouput.UvAnimLookup.Count() == 0)
                    {
                        M2Ouput.UvAnimLookup.Add(-1);
                    }

                    // TODO : texture animations, particles, ribbons, ligts...

                    //////////////// write m2 file ///////////////////////
                    string m2path = Path.GetDirectoryName(fileName) + "\\" + M2Ouput.Name + ".m2";
                    using (var stream = File.Open(m2path, FileMode.Create))
                    {
                        using (var writer = new BinaryWriter(stream, Encoding.UTF8, false))
                        {
                            Console.WriteLine("Saving " + m2path + "...");
                            M2Ouput.Save(writer, M2.Format.LichKing);
                            Console.WriteLine("Saved !");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("filepath not valid, skipping : " + fileName);
                }
            }

            Console.ReadLine(); // keep console open until a key is pressed
        }
    }
}