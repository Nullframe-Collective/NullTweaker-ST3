using NullTweaker.Core;
using System.Text.Json;
using System.Text.Json.Nodes;
using CustomWeaponsFramework;
using CustomNPCFramework;
using CustomMapsFramework;
using CustomPlayerCustomization;

int checks=0;
void Check(bool value,string name){if(!value)throw new Exception(name);checks++;}
void Fails(Action action,string name){try{action();}catch(Exception){checks++;return;}throw new Exception(name);}
string temp=Path.Combine(Path.GetTempPath(),"NullTweaker-checks-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temp);
string Write(string name,string text){string path=Path.Combine(temp,name);File.WriteAllText(path,text);return path;}
JsonSerializerOptions options=new(){IncludeFields=true,PropertyNameCaseInsensitive=true};
try
{
 foreach(DocumentKind kind in Enum.GetValues<DocumentKind>())
 {
  ConfigDocument doc=new(kind);Check(Validator.Check(doc).Count==0,"valid new "+kind);
  string path=Path.Combine(temp,Schema.FileName(kind));doc.Save(path);Check(!doc.Dirty,"saved state "+kind);
  ConfigDocument loaded=ConfigDocument.Open(path);Check(JsonNode.DeepEquals(doc.Root,loaded.Root),"round trip "+kind);
 }
 ConfigDocument customization=new(DocumentKind.Customization);
 JsonSerializerOptions customizationJson=new(){Converters={new VectorJsonConverter()}};
 CustomizationInfo appearance=JsonSerializer.Deserialize<CustomizationInfo>(customization.Json(),customizationJson);
 Check(appearance.Name=="MyCustomization"&&appearance.IsAvailable&&appearance.Price==0,"customization template matches real model");
 Check(appearance.MainMenuTransform.Scale.x==1&&appearance.InGameTransform.Scale.z==1,"customization default scales are identity");
 Check(appearance.MainMenuTransform.Position.y==0&&appearance.InGameTransform.Rotation.x==0,"customization default transforms are zero");
 customization.Root["Author"]="ContentAuthor";customization.Root["Price"]=12345;customization.Root["IsAvailable"]=false;
 customization.Root["MainMenuTransform"]["Scale"]=new JsonArray(-2,3,4);
 customization.Root["InGameTransform"]["LocalPosition"]=new JsonArray(0.1,-55.3,0.25);
 appearance=JsonSerializer.Deserialize<CustomizationInfo>(customization.Json(),customizationJson);
 Check(appearance.Author=="ContentAuthor"&&appearance.Price==12345&&!appearance.IsAvailable,"customization identity and availability serialize correctly");
 Check(appearance.MainMenuTransform.Scale.x==-2&&appearance.InGameTransform.LocalPosition.y==-55.3f,"signed scale and per-context transforms serialize correctly");
 Check(Validator.Check(customization).Count==0,"signed customization transforms accepted");
 string pumpkinPath=Path.Combine(AppContext.BaseDirectory,"Fixtures","SuperPumpkin.json");
 string pumpkinText=File.ReadAllText(pumpkinPath);
 var pumpkin=ConfigDocument.Open(Write("pumpkin-copy.json",pumpkinText));
 Check(pumpkin.Kind==DocumentKind.Customization&&!pumpkin.Dirty,"SuperPumpkin detected by shape without altering it");
 Check(Validator.Check(pumpkin).Count==0,"actual SuperPumpkin config accepted");
 var originalAppearance=JsonSerializer.Deserialize<CustomizationInfo>(pumpkinText,customizationJson);
 appearance=JsonSerializer.Deserialize<CustomizationInfo>(pumpkin.Json(),customizationJson);
 Check(appearance.Name==originalAppearance.Name&&appearance.Author==originalAppearance.Author&&
  appearance.MainMenuTransform.Scale.y==originalAppearance.MainMenuTransform.Scale.y&&
  appearance.InGameTransform.LocalPosition.z==originalAppearance.InGameTransform.LocalPosition.z,"SuperPumpkin transform values survive real converter round trip");
 pumpkin.Root["MainMenuTransform"]["Extension"]=new JsonObject{["KeepMe"]=true};
 string customizationPath=Path.Combine(temp,"customization.json");pumpkin.Save(customizationPath);
 var reopened=ConfigDocument.Open(customizationPath);
 Check(JsonNode.DeepEquals(pumpkin.Root,reopened.Root),"customization save/open preserves nested unknown fields and all vectors");
 Check(File.ReadAllText(pumpkinPath)==pumpkinText,"original installed SuperPumpkin file unchanged");
 var lower=ConfigDocument.Open(Write("lower-hat.json","""{"name":"LegacyHat","price":0,"mainMenuTransform":{"localPosition":[1,2,3],"scale":[1,1,1]},"inGameTransform":{"rotation":[90,0,0]}}"""));
 Check(lower.Kind==DocumentKind.Customization&&lower.Root["MainMenuTransform"]["LocalPosition"][0].GetValue<int>()==1,"nested customization keys canonicalized");
 Check(lower.Notices.Count>0&&Validator.Check(lower).Count==0,"legacy customization conversion notes and validation");
 var minimal=ConfigDocument.Open(Write("customization.json","""{"Name":"MinimalHat"}"""));
 Check(minimal.Kind==DocumentKind.Customization&&Validator.Check(minimal).Count==0,"minimal customization recognised by filename");
 appearance=JsonSerializer.Deserialize<CustomizationInfo>(minimal.Json(),customizationJson);
 Check(appearance.MainMenuTransform.Scale.y==1&&appearance.InGameTransform.Scale.x==1,"omitted customization transforms retain framework defaults");
 customization.Root["Id"]="base:reserved";
 Check(Validator.Check(customization).Any(e=>e.Contains("reserved")),"native customization namespace protected");
 customization.Root["Id"]="author:hat";customization.Root["Price"]=-1;
 Check(Validator.Check(customization).Count>0,"negative customization price rejected");
 customization.Root["Price"]=1.5;
 Check(Validator.Check(customization).Count>0,"fractional customization price rejected");
 customization.Root["Price"]=0;customization.Root["MainMenuTransform"]["Rotation"]=new JsonArray(1,2);
 Check(Validator.Check(customization).Any(e=>e.Contains("MainMenuTransform.Rotation")),"malformed nested vector identified by full path");
 customization.Root["MainMenuTransform"]["Rotation"]=new JsonArray(1,"NaN",3);
 Fails(()=>customization.Save(Path.Combine(temp,"invalid-customization.json")),"invalid customization vector cannot be saved");
 customization.Root["MainMenuTransform"]=false;
 Check(Validator.Check(customization).Any(e=>e.Contains("MainMenuTransform")),"invalid transform object rejected");
 Fails(()=>ConfigDocument.Open(Write("duplicate-customization.json","""{"Name":"Hat","MainMenuTransform":{"Scale":[1,1,1],"scale":[2,2,2]}}""")),"duplicate nested transform fields rejected");
 ConfigDocument weapon=new(DocumentKind.Weapon);
 var typed=JsonSerializer.Deserialize<WeaponInfo>(weapon.Json());
 Check(typed.Name=="MyWeapon"&&typed.CanAim&&typed.BulletDamage==15&&typed.BulledSpeed==500,"current weapon wire format");
 ((JsonObject)weapon.Root)["UseScope"]=true;((JsonObject)weapon.Root)["BulletsPerClip"]=7;
 Check(Validator.Check(weapon).Count==0,"editable integer JSON value accepted");
 Check(JsonSerializer.Deserialize<WeaponInfo>(weapon.Json()).UseScope,"scope follows checkbox value");
 var legacyWeapon=ConfigDocument.Open(Write("old-weapon.json","""{"name":"Legacy","type":2,"animationType":1,"aimPosX":0.5,"bulledSpeed":450,"VendorData":{"flag":true}}"""));
 typed=JsonSerializer.Deserialize<WeaponInfo>(legacyWeapon.Json());
 Check(typed.Name=="Legacy"&&typed.AimPositionX==0.5f&&typed.Type==Il2Cpp.WeaponScript.DOEEBEFKIJI.SHOTGUN,"legacy weapon migration");
 Check(legacyWeapon.Root["VendorData"]["flag"].GetValue<bool>(),"unknown extension preserved");
 Check(legacyWeapon.Dirty&&legacyWeapon.Notices.Count>0,"migration explicitly recorded");
 var npc=new ConfigDocument(DocumentKind.NPC);
 foreach(string type in Schema.NPCTypes)
 {
  npc.SelectNPCType(type);var data=(JsonObject)npc.Root[type];data["DummyNPC"]="verified/native-prefab";
  Check(Validator.Check(npc).Count==0,"NPC validation "+type);
  var info=JsonSerializer.Deserialize<NPCInfo>(npc.Json(),options);
  Check(new object[]{info.Bot,info.BossBot,info.CustardBot,info.PlayerMonster}.Count(v=>v!=null)==1,"one payload "+type);
  File.WriteAllText(Path.Combine(temp,"npc.json"),npc.Json());
  Check(NPCManifest.Read(temp).Name=="MyMonster","actual NPC manifest accepts "+type);
 }
 npc.SelectNPCType("Bot");((JsonObject)npc.Root["Bot"])["Health"]=789;
 npc.SelectNPCType("BossBot");npc.SelectNPCType("Bot");
 Check(npc.Root["Bot"]["Health"].GetValue<int>()==789,"type drafts retained");
 var oldNPC=ConfigDocument.Open(Write("legacy-npc.json","""{"name":"Old","version":"1.0","bot":{"dummyNPC":"SUR/Newborn_Bot","hp":200,"damage":10,"speed":2,"attackRange":2,"attackSpeed":1},"bossBot":{"dummyNPC":"BossTinky","hp":1000}}"""));
 Check(Validator.Check(oldNPC).Any(e=>e.Contains("exactly one")),"old multi-type NPC requires explicit choice");
 oldNPC.SelectNPCType("Bot");Check(Validator.Check(oldNPC).Count==0,"legacy NPC repaired after explicit choice");
 Check(JsonSerializer.Deserialize<NPCInfo>(oldNPC.Json(),options).Bot.Health==200,"legacy hp converted to Health");
 var map=new ConfigDocument(DocumentKind.Map);
 var mapTyped=JsonSerializer.Deserialize<MapJson>(map.Json(),options);
 Check(mapTyped.MapName=="My Map"&&mapTyped.Dependencies.Count==0&&mapTyped.VersusMonster=="Newborn","current map wire format");
 var imported=ConfigDocument.ImportIni("[CustomMap]\nmapName=Legacy Map\nuseDayAndNight=True\nsize=2\ndependencies=one|two|\n");
 mapTyped=JsonSerializer.Deserialize<MapJson>(imported.Json(),options);
 Check(mapTyped.UseDayAndNight&&mapTyped.Size==2&&mapTyped.Dependencies.SequenceEqual(new[]{"one","two"}),"legacy map INI import");
 var waves=ConfigDocument.Open(Write("old-waves.json","""[{"maxNPC":15,"waveInfo":{"totalCount":10,"defaultNPC":"Newborn_Bot","npc":[{"npcName":"BossPo","spawnChance":25,"isBoss":true}]}}]"""));
 var waveTyped=JsonSerializer.Deserialize<List<CustomWave>>(waves.Json(),options);
 Check(waveTyped[0].WaveInfo.NPCs[0].NPCName=="BossPo","old npc array converted to NPCs");
 Check(waveTyped[0].WaveInfo.NPCs[0].SpawnChance==25&&Validator.Check(waves).Count==0,"valid migrated wave");
 var triggers=new ConfigDocument(DocumentKind.Triggers);
 ((JsonArray)triggers.Root).Add(Schema.Object(Schema.Trigger));
 var triggerTyped=JsonSerializer.Deserialize<TriggerSettings[]>(triggers.Json(),options);
 TriggerSettings.ValidateAll(triggerTyped);Check(Validator.Check(triggers).Count==0,"actual trigger schema accepts export");
 foreach(string type in new[]{"Event","Damage","Kill","Water","Ambient"})
 {
  triggers.Root[0]["Type"]=type;
  TriggerSettings.ValidateAll(JsonSerializer.Deserialize<TriggerSettings[]>(triggers.Json(),options));checks++;
 }
 ((JsonArray)triggers.Root).Add(triggers.Root[0].DeepClone());
 Check(Validator.Check(triggers).Any(e=>e.Contains("duplicate")),"duplicate trigger ID blocked");
 ((JsonArray)triggers.Root).RemoveAt(1);triggers.Root[0]["Size"]=new JsonArray(1,0,1);
 Check(Validator.Check(triggers).Count>0,"zero size blocked");
 ((JsonObject)weapon.Root)["FireRate"]="NaN";Check(Validator.Check(weapon).Count>0,"invalid numeric text blocked");
 Fails(()=>weapon.Save(Path.Combine(temp,"invalid.json")),"invalid save blocked");
 Fails(()=>ConfigDocument.Open(Write("dup.json","""{"Name":"One","name":"Two"}""")),"duplicate keys rejected");
 Fails(()=>ConfigDocument.Open(Write("conflict.json","""{"name":"A","aimPosX":1,"AimPositionX":2}""")),"conflicting aliases rejected");
 Fails(()=>ConfigDocument.Open(Write("bad.json","{")),"malformed JSON rejected");
 var saved=new ConfigDocument(DocumentKind.Map);string savedPath=Path.Combine(temp,"conflict-map.json");saved.Save(savedPath);
 File.AppendAllText(savedPath," ");Fails(()=>saved.Save(savedPath),"external modification protected");
 Check(ConfigDocument.Open(Write("triggers.json","[]")).Kind==DocumentKind.Triggers,"empty triggers import");
 var emptyWaves=ConfigDocument.Open(Write("waves.json","[]"));Check(Validator.Check(emptyWaves).Count>0,"empty waves repairable");
 Check(!Directory.GetFiles(temp,"*.tmp").Any(),"no stale atomic-save files");
 Console.WriteLine($"{checks} checks passed, including deserialization with the actual framework model sources.");
}
finally {Directory.Delete(temp,true);}
