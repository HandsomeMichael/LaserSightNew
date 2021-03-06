using Microsoft.Xna.Framework; //Vector2
using Microsoft.Xna.Framework.Graphics; //SpriteBatch | Texture2D
using Newtonsoft.Json; //JsonConverterAttribute
using Newtonsoft.Json.Converters; //StringEnumConverter
using System;// basic stuff like Math class
using System.Collections.Generic; // List<>
using System.ComponentModel; //DefaultValueAttribute
using System.Reflection; // reflection ( no way)
using Terraria; //Main.LocalPlayer/Main.Player | Item
using Terraria.DataStructures; // structure
using Terraria.GameInput; // hotkey
using Terraria.Graphics; // graphics
using Terraria.Graphics.Shaders; // shader
using Terraria.ID; // id
using Terraria.ModLoader; //Mod
using Terraria.ModLoader.Config; //ModConfig
using Terraria.ModLoader.Config.UI; //ModConfigUI
using Terraria.UI; //UIMouseEvent

namespace LaserSightNew
{
	public class LaserSightNew : Mod
	{
		internal static ModHotKey ToggleKey;
		internal static ModHotKey ToggleAimKey;
		internal static ModHotKey AimKey;
		internal static ModHotKey QuickEnemy;
		internal static ModHotKey ToggleQuickEnemy;
		
		public override void Load ()
		{
            ToggleKey = RegisterHotKey("Quick Toggle Laser", "P");
			ToggleAimKey = RegisterHotKey("Quick Toggle Lock-On Aiming", "T");
			AimKey = RegisterHotKey("Quick Lock-On Aiming", "Y");
			QuickEnemy = RegisterHotKey("Quick Lock-On Nearest Enemy", "K");
			ToggleQuickEnemy = RegisterHotKey("Quick Toggle Lock-On Nearest Enemy", "L");
		}
		public override void PostSetupContent() {
			var obama = ModLoader.GetMod("ObamaCamera");
			if (obama != null && LaserConfig.get.obamaMod) {
				// yes this is complex but its better than il edit and reflection, i call this "il edit replacement"
				obama.Call("typeModifier",(Func<string>)(() => ((LaserToggle.Aim && LaserSight.hitmark != -1) ? "Enemy and Player" : "None") ));
				obama.Call("loopModifier",(Func<int>)(() => ((LaserToggle.Aim && LaserSight.hitmark != -1) ? LaserSight.hitmark : -2) ),
				(Func<bool>)(() => ((LaserToggle.Aim && LaserSight.hitmark != -1) ? false : true) ));
			}
		}
		public override void Unload() {
			AimKey = null;
			ToggleKey = null;
			ToggleAimKey = null;
			QuickEnemy = null;
			ToggleQuickEnemy = null;
		}
	}
	
	public class LaserToggle : ModPlayer
	{
		public static bool Aim;
		public static bool AutoAim = false;
		public static bool HACKSREPORTED = false;
		public override void ProcessTriggers (TriggersSet triggersSet)
		{
			Aim = false;
			if (LaserSightNew.ToggleKey.JustPressed)
			{
				LaserConfig.get.laserEnabled = !LaserConfig.get.laserEnabled;
				LaserConfig.SaveConfig();
				CombatText.NewText(player.getRect(),(LaserConfig.get.laserEnabled ? Color.LightGreen : Color.Pink),(LaserConfig.get.laserEnabled ? "Laser Enabled" : "Laser Disabled"));
			}
			if (LaserSightNew.ToggleAimKey.JustPressed)
			{
				AutoAim = !AutoAim;
				CombatText.NewText(player.getRect(),(AutoAim ? Color.LightGreen : Color.Pink),(AutoAim ? "Lock-On Aiming Enabled" : "Lock-On Aiming Disabled"));
			}
			if (LaserSightNew.ToggleQuickEnemy.JustPressed)
			{
				HACKSREPORTED = !HACKSREPORTED;
				CombatText.NewText(player.getRect(),(HACKSREPORTED ? Color.LightGreen : Color.Pink),(HACKSREPORTED ? "Lock-On Nearest Enemy Enabled" : "Lock-On Nearest Enemy Disabled"));
			}
			if (LaserSightNew.QuickEnemy.JustPressed || HACKSREPORTED) {
				int index = -1;
				Vector2 targetCenter = player.Center;
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC npc = Main.npc[i];
					if (npc.CanBeChasedBy()) {
						float between = Vector2.Distance(npc.Center, player.Center);
						bool closest = Vector2.Distance(player.Center, targetCenter) > between;
						bool inRange = between < 700f;
						bool lineOfSight = Collision.CanHitLine(player.Center, 1, 1, npc.position, npc.width, npc.height);
						bool closeThroughWall = between < 100f;
						if ((closest || index == -1) && inRange && (lineOfSight || closeThroughWall)) {
							targetCenter = npc.Center;
							index = i;
						}
					}
				}
				if (index != -1) {
					LaserSight.hitmark = index;
					if (!AutoAim) {AutoAim = true;}
				}
				
			}
			if (LaserSightNew.AimKey.Current || AutoAim) {
				if (LaserSight.hitmark != -1) {
					NPC npc = Main.npc[LaserSight.hitmark];
					if (npc.active) {
						Aim = true;
						Main.mouseX = (int)(npc.Center.X + (npc.velocity.X*LaserConfig.get.laserEnemyPredict) - Main.screenPosition.X);
						Main.mouseY = (int)(npc.Center.Y + (npc.velocity.Y*LaserConfig.get.laserEnemyPredict) - Main.screenPosition.Y);
					}
				}
			}
		}
	}
	
	class LaserSight : ModWorld
	{
		//hitmark , the index of targeted enemy asdsdsd
		public static int hitmark;
		public override void PostDrawTiles() {
			DrawPlayerLaser(Main.LocalPlayer);
		}
		public void DrawPlayerLaser (Player player)
		{
			//dont run on ded or unactive player
			if (!player.active || player.dead) {return;}
			// have laser scope to make it a bit balanced i guess
			if (!Main.gamePaused && LaserConfig.get.laserEnabled && ((player.scope && LaserConfig.get.laserScope) || !LaserConfig.get.laserScope))
			{
				if (player.HeldItem.ranged || LaserConfig.get.anyWeap)
				{
					/*

					Use different type of draw lines

					Color startColor = LaserConfig.get.startColor;
					Color endColor = LaserConfig.get.endColor;
					//Color color = Color.Red;
					Vector2 position = Main.MouseWorld;
					float zoom = Main.GameZoomTarget;
					float scale;
					
					if (LaserConfig.get.scaleAuto)
					{scale = zoom + 1f;}
					else
					{scale = LaserConfig.get.scale;}
					//float scale = zoom + 1f;
					
					Main.spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, Main.GameViewMatrix.ZoomMatrix);
					SpriteBatch spriteBatch = Main.spriteBatch;
					DrawLine (Main.spriteBatch, player.Center, position, startColor, endColor, scale);
					Main.spriteBatch.End ();

					*/

					// color setup (laserColor = ColorA | markerColor = ColorB)
					Color colorA = (LaserConfig.get.laserDisco ? Main.DiscoColor : LaserConfig.get.laserColor);
					//Color colorB = (LaserConfig.get.markerDisco ? Main.DiscoColor : LaserConfig.get.markerColor);
					
					//Separate color setup for Target-Marker
					Color colorB;
					if (LaserConfig.get.markerColorLaser == false)
					{
						if (LaserConfig.get.markerDisco == true && LaserConfig.get.laserDisco == false)
						{
							//Applies Disco Effect to Target-Marker
							colorB = Main.DiscoColor;
						}
						else
						{
							//Target-Marker will use markerColor
							colorB = LaserConfig.get.markerColor;
						}
					}
					else
					{
						//Target-Marker will use laserColor
						colorB = colorA;
					}

					// if npc is gone , set hitmark to -1 
					if (hitmark > -1) {
						NPC npc = Main.npc[hitmark];
						if (!npc.active || npc.dontTakeDamage || npc.townNPC || npc.friendly ||  npc.damage == 0 || npc.lifeMax < 5) {
							hitmark = -1;
						}
					}

					// only accept valid index
					if (LaserConfig.get.laserEnemy && hitmark > -1) {

						//the npc
						NPC npc = Main.npc[hitmark];

						//start
						// amongus sussy wussy
						if (LaserConfig.get.laserDye != null && LaserConfig.get.laserDye.Type > 1) {
							var shader = GameShaders.Armor.GetShaderIdFromItemId(LaserConfig.get.laserDye.Type);
							if (shader != null) {
								Main.spriteBatch.Begin(SpriteSortMode.Immediate, null, null, null, null, null, Main.GameViewMatrix.ZoomMatrix);
								GameShaders.Armor.Apply(shader, player, null);
							}
							else {
								Main.spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, Main.GameViewMatrix.ZoomMatrix);	
							}
						}
						else {
							Main.spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, Main.GameViewMatrix.ZoomMatrix);
						}

						//texture
						string path = "LaserSightNew/Texture/"+(LaserConfig.get.smooth ? "Smooth/" : "Rough/");
						
						Texture2D texture = ModContent.GetTexture(path+LaserConfig.get.markerTexture);

						//do some sinwave scale crap
						float scale = (float)Math.Sin(Main.GameUpdateCount/60f);
						if (scale < 0f) {scale *= -1f;}
						scale += 0.3f;

						//draw the hitmark
						float rot = MathHelper.ToRadians(Main.GameUpdateCount)*1.5f;
						if (!LaserConfig.get.markerSpin) {rot = 0f;}
						if (!LaserConfig.get.markerPulse) {scale = 1f;}
						Main.spriteBatch.Draw(texture, npc.Center + (npc.velocity*LaserConfig.get.laserEnemyPredict) - Main.screenPosition, null, colorB, 
						rot, texture.Size()/2f, scale, SpriteEffects.None, 0);

						//end
						Main.spriteBatch.End();
					}

					// start with blendstate.additive abuse
					if (LaserConfig.get.laserDye != null && LaserConfig.get.laserDye.Type > 1) {
						var shader = GameShaders.Armor.GetShaderIdFromItemId(LaserConfig.get.laserDye.Type);
						if (shader != null) {
							Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, null, null, null, null, Main.GameViewMatrix.ZoomMatrix);
							GameShaders.Armor.Apply(shader, player, null);
						}
						else {
							Main.spriteBatch.Begin (SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, Main.GameViewMatrix.ZoomMatrix);	
						}
					}
					else {
						Main.spriteBatch.Begin (SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, Main.GameViewMatrix.ZoomMatrix);
					}

					//setup some variable
					Vector2 endPoint = player.Center;
					Vector2 laserStart = player.Center;

					//check where already done in before
					bool run = true;
					if (LaserToggle.Aim) {
						if (hitmark != -1) {
							run = false;
							NPC npc = Main.npc[hitmark];
							Main.mouseX = (int)(npc.Center.X + (npc.velocity.X*LaserConfig.get.laserEnemyPredict) - Main.screenPosition.X);
							Main.mouseY = (int)(npc.Center.Y + (npc.velocity.Y*LaserConfig.get.laserEnemyPredict) - Main.screenPosition.Y);
							endPoint = npc.Center + (npc.velocity*LaserConfig.get.laserEnemyPredict);
						}
					}

					float LaserRotation = (Main.MouseWorld - laserStart).ToRotation();
					Vector2 offset = new Vector2(5,5);

					// dont run searching laser end if it auto aim to prevent draw bug
					if (run) {
						if (LaserConfig.get.laserMouse) {
						// this one is a bit offset and i tried doing stuff but it didnt work
							while (!PointInTile(endPoint) && Vector2.Distance(endPoint,Main.MouseWorld + offset) > 20f){
								endPoint += Vector2.Normalize((Main.MouseWorld + offset) - endPoint)*8f;
							}
						}
						else {
							//awawawawawawawawawawawawawa
							for (int k = 0; k < LaserConfig.get.laserLimit; k++)
							{
								Vector2 posCheck = endPoint + Vector2.UnitX.RotatedBy(LaserRotation) * k * 8;
								if (PointInTile(posCheck) || k == 159)
								{
									endPoint = posCheck;
									break;
								}
							}
						}
						// this is how the laser stops at npcs, it checks for the closest npc that collide with the laser
						if (LaserConfig.get.laserCollision == "NPCs & Tiles" || LaserConfig.get.laserEnemy)
						{
							int b = -1;
							for (int i = 0; i < Main.maxNPCs; i++)
							{
								NPC npc = Main.npc[i];
								if (npc.active && !npc.townNPC && !npc.friendly && npc.damage > 0 && npc.lifeMax > 5) {
									float between = Vector2.Distance(npc.Center, laserStart);
									bool closest = Vector2.Distance(laserStart, endPoint) > between;
									var targetHitbox = npc.Hitbox;
									float point = 0f;
									// let collison do all the magic , honestly i dont even know how does this work without destroying frame rate
									bool collide = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), laserStart,endPoint, 5, ref point);
									if ((closest || b == -1 ) && collide) {
										b = i;
										if (LaserConfig.get.laserCollision == "NPCs & Tiles") {endPoint = npc.Center;}
										if (LaserConfig.get.laserEnemy) {
											hitmark = i;
										}
									}
								}
							}
						}
					}

					//variable setup part 2					
					var texBeam = ModContent.GetTexture("LaserSightNew/GlowTrail");
					Vector2 origin = new Vector2(0, texBeam.Height / 2);
					float height = LaserConfig.get.scale;
					if (LaserConfig.get.scaleAuto){height = Main.GameZoomTarget*5f;}
					int width = (int)(laserStart - endPoint).Length() - 24;
					var pos = laserStart - Main.screenPosition + Vector2.UnitX.RotatedBy(LaserRotation) * 24;
					var target = new Rectangle((int)pos.X, (int)pos.Y, width, (int)(height * 1.2f));

					// draw with rectangle and rotation
					Main.spriteBatch.Draw(texBeam, target, null, colorA, LaserRotation, origin, 0, 0);

					//lighting
					if (LaserConfig.get.laserLight) {
						for (int i = 0; i < width; i += 10) {
							Lighting.AddLight(pos + Vector2.UnitX.RotatedBy(LaserRotation) * i + Main.screenPosition, colorA.ToVector3() * height * 0.030f);         
						}
					}

					//end
					Main.spriteBatch.End();
				}
				// if player not holding other weapon, reset
				else {
					hitmark = -1;
				}
				
			}
			// if disabled or game not runned , reset
			else {
				hitmark = -1;
			}
			
		}
		// check if the point is colliding a tile
		public static bool PointInTile(Vector2 point)
        {
			// no collison , awa
			if (LaserConfig.get.laserCollision == "No Collision") {return false;}
            Point16 startCoords = new Point16((int)point.X / 16, (int)point.Y / 16);
            for(int x = -1; x <= 1; x++)
                for(int y = -1; y <= 1; y++)
                {                 
                    var thisPoint = startCoords + new Point16(x, y);

                    if (!WorldGen.InWorld(thisPoint.X, thisPoint.Y)) return false;

                        var tile = Framing.GetTileSafely(thisPoint);
                    if(tile.collisionType == 1 && !tile.inActive())
                    {
                        var rect = new Rectangle(thisPoint.X * 16, thisPoint.Y * 16, 16, 16);
                        if (rect.Contains(point.ToPoint())) return true;
                    }
                }

            return false;
        }
		
		public void DrawLine (SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color colorStart, Color colorEnd, float width)
		{
			float num = Vector2.Distance(start, end);
			Vector2 vector = (end - start) / num;
			Vector2 value = start;
			Vector2 screenPosition = Main.screenPosition;
			float rotation = vector.ToRotation();
			float scale = width / 16f;
			for (float num2 = 0f; num2 <= num; num2 += width)
			{
				float amount = num2 / num;
				spriteBatch.Draw (Main.blackTileTexture, value - screenPosition, null, Color.Lerp(colorStart, colorEnd, amount), rotation, Vector2.Zero, scale, SpriteEffects.None, 0f);
				value = start + num2 * vector;
			}
		}
	}
	
	[Label("Configs")]
	public class LaserConfig : ModConfig
	{
		//Saves the config
		public static void SaveConfig ()
		{typeof(ConfigManager).GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[1] { get });}

		public override ConfigScope Mode => ConfigScope.ClientSide;
		public static LaserConfig get => ModContent.GetInstance<LaserConfig>();
		
		[Header("Laser Settings")]
		
		[Label("Laser Enabled")]
		[Tooltip("Whether a laser appears or not")]
		[DefaultValue(true)]
		public bool laserEnabled;
		
		[Label("Show laser in any weapons or unarmed")]
		[Tooltip("Allows laser to show in any weapon types, includes unarmed")]
		[DefaultValue(false)]
		public bool anyWeap;
		
		[Label("Laser-only with scope")]
		[Tooltip("Activate laser only when using a sniper scope")]
		[DefaultValue(false)]
		public bool laserScope;
		
		[Label("Laser Collision Type")]
		[Tooltip("Change how laser works with NPCs and Tiles")]
		[OptionStrings(new string[] { "NPCs & Tiles", "Tiles Only", "No Collision"})]
		[DefaultValue("NPCs & Tiles")]
		[DrawTicks]
		public string laserCollision;
		
		[Label("Limit laser distance to mouse cursor")]
		[Tooltip("Limit laser length to mouse, and ignores 'Laser Distance Limit'")]
		[DefaultValue(false)]
		public bool laserMouse;
		
		[Label("Laser Distance Limit")]
		[Tooltip("The distance limit of the laser")]
		[Range(30, 1000)]
		[Increment(10)]
		[DefaultValue(500)]
		[Slider] 
		public int laserLimit;
		
		[Label("Laser Auto-Scale")]
		[Tooltip("Adjust laser width based on camera zoom, and ignores 'Laser Auto-Scale'")]
		[DefaultValue(true)]
		public bool scaleAuto;
		
		[Label("Laser Scale")]
		[Tooltip("Change how thin or WIDE the laser could get")]
		[Range(1, 20)]
		[Increment(1)]
		[DefaultValue(10)]
		[Slider] 
		public int scale;
		
		[Label("Laser Light")]
		[Tooltip("Make laser emmit light")]
		[DefaultValue(true)]
		public bool laserLight;
		
		[Label("Laser Dye")]
		[Tooltip("The laser custom dye usage\nMust be a valid dye !")]
		public ItemDefinition laserDye = new ItemDefinition("Terraria GoldOre");

		[Label("Laser Color")]
		[Tooltip("The color of the laser")]
		[DefaultValue(typeof(Color), "255,100,0,255")]
		public Color laserColor = new Color(255,100,0,255);

		[Label("Laser Disco Effect")]
		[Tooltip("Cycle between colors\nDoes not include amongus color")]
		[DefaultValue(false)]
		public bool laserDisco;
		
		
		[Header("Target-Mark Settings")]
		
		[Label("Target-Marking Enabled")]
		//Reserved	//[Tooltip("Mark targets if a laser/mouse points to an enemy\nCan be used along with Lock-On Aiming in the hotkey configs.")]
		[Tooltip("Mark targets if a laser points to an enemy\nCan be used along with Lock-On Aiming in the hotkey configs.")]
		[DefaultValue(true)]
		public bool laserEnemy;
		
		//Reserved
		/*[Label("Target-Marking with Mouse")]
		[Tooltip("Marks target with mouse cursor instead of laser")]
		[DefaultValue(false)]
		public bool markTargetMouse;*/

		[Label("Lock-On Predicted-Aim Accuracy")]
		[Tooltip("The intensity of aim lock predict enemy movement\nMay not work against enemies with custom movement")]
		[Range(0, 10)]
		[Increment(1)]
		[DefaultValue(0)]
		[Slider] 
		public int laserEnemyPredict;
		
		[Label("Share color with laser")]
		[Tooltip("Uses laser color instead of using separate color")]
		[DefaultValue(true)]
		public bool markerColorLaser;
		
		[Label("Target-Marker Color")]
		[Tooltip("The separate color for the target-marker")]
		[DefaultValue(typeof(Color), "255,100,0,255")]
		public Color markerColor = new Color(255,100,0,255);
		
		[Label("Target-Marker Disco Effect")]
		[Tooltip("Separated disco effect for target-marker\nOnly works if both 'laser color sharing' and 'laser disco effect' were disabled")]
		[DefaultValue(false)]
		public bool markerDisco;
		
		[Label("Target-Marker Texture")]
		[DefaultValue(TextureEnum.Marker12)]
		[Tooltip("Texture for Target-Marker\n(Default: Marker12)")]
		public TextureEnum markerTexture;

		[Label("Target-Marker Animate: Spin")]
		[Tooltip("Allow the marker to spin")]
		[DefaultValue(true)]
		public bool markerSpin;
		
		[Label("Target-Marker Animate: Pulse")]
		[Tooltip("Allow the marker to pulse")]
		[DefaultValue(true)]
		public bool markerPulse;
		
		[Label("Smooth Texture")]
		[Tooltip("Make the target-marker uses smooth texture")]
		[DefaultValue(false)]
		public bool smooth;

		[Label("Obama Camera Overhaul Cross Mod")]
		[Tooltip("Make targeted enemy slightly focused\nrequires a reload to change\nrequires obama camera overhaul mod v0.8.7 or higher")]
		[DefaultValue(false)]
		public bool obamaMod;
		
		//Reserved
		/*[Label("Target-Marker Animate: Spin")]
		[Tooltip("Allow the marker to spin")]
		[DefaultValue(false)]
		public bool markerSpin;
		
		[Label("Target-Marker Animate: Pulse")]
		[Tooltip("Allow the marker to pulse")]
		[DefaultValue(false)]
		public bool markerPulse;*/
		
		//Auto-Aim blacklist
		//Critter
		//TownNPC
		//Target Dummy
		
		//Custom Target-Marker Texture
		[JsonConverter(typeof(StringEnumConverter))]
		[CustomModConfigItem(typeof(TextureElement))]
		
		public enum TextureEnum
		{
			Marker01,
			Marker02,
			Marker03,
			Marker04,
			Marker05,
			Marker06,
			Marker07,
			Marker08,
			Marker09,
			Marker10,
			Marker11,
			Marker12,
			Marker13
		}
		
		internal class TextureElement : ConfigElement
		{
			Texture2D markerTexture;
			string[] valueStrings;
			
			public override void OnBind ()
			{
				base.OnBind ();
				markerTexture = Terraria.Graphics.TextureManager.Load("Images/UI/Settings_Toggle");
				valueStrings = Enum.GetNames (memberInfo.Type);
				TextDisplayFunction = () => memberInfo.Name + ": " + GetStringValue ();
				if (labelAttribute != null)
				{
					TextDisplayFunction = () => labelAttribute.Label + ": " + GetStringValue ();
				}
			}
			
			void SetValue(TextureEnum value) => SetObject(value);

			TextureEnum GetValue() => (TextureEnum)GetObject();
			
			string GetStringValue()
			{
				return valueStrings[(int)GetValue()].Replace("_"," ");
			}
			
			public override void Click(UIMouseEvent evt)
			{
				base.Click(evt);
				SetValue(GetValue().NextEnum());
			}

			public override void RightClick(UIMouseEvent evt)
			{
				base.RightClick(evt);
				SetValue(GetValue().PreviousEnum());
			}
			
			static float timer;
			
			public override void Draw(SpriteBatch spriteBatch)
			{
				base.Draw(spriteBatch);
				timer += 0.5f;
				
				//reset at 696969f
				if (timer >= 696969f)
				{timer = 0;}
			
				string path = "LaserSightNew/Texture/"+(LaserConfig.get.smooth ? "Smooth/" : "Rough/");
				path += GetStringValue().Replace(" ","_");
				int frame = (int)timer % 25;
				frame /= 1;
				CalculatedStyle dimensions = base.GetDimensions();
				Texture2D texture = ModContent.GetTexture(path);
				Vector2 pos = new Vector2(dimensions.X + dimensions.Width  -36,dimensions.Y + 15);
				spriteBatch.Draw(texture, pos, null, Color.White, 0f, texture.Size()/2f, 0.7f, SpriteEffects.None, 0);
			}
		}
	}
	
	/*public class hitmarkerColorData
	{
		[Header("Laser Color")]

		[Label("Laser Color")]
		[DefaultValue(typeof(Color), "255,0,0,255")]
		public Color LaserConfig.get.laserColor = new Color(255,0,0,255);
	}*/
}