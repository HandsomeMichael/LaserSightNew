using Microsoft.Xna.Framework; //Vector2
using Microsoft.Xna.Framework.Graphics; //SpriteBatch | Texture2D
using System.ComponentModel; //DefaultValueAttribute
using Terraria; //Main.LocalPlayer/Main.Player | Item
using Terraria.ModLoader; //Mod
using Terraria.ModLoader.Config; //ModConfig
using Terraria.ModLoader.Config.UI; //ModConfigUI
using System;// basic stuff like Math class
using System.Collections.Generic; // List<>
using Terraria.DataStructures; // structure
using Terraria.Graphics; // graphics
using Terraria.ID; // id
using Terraria.GameInput; // hotkey
using System.Reflection; // reflection ( no way)

namespace LaserSightNew
{
	public class LaserSightNew : Mod
	{
		internal static ModHotKey ToggleKey;
		internal static ModHotKey ToggleAimKey;
		internal static ModHotKey AimKey;
		
		public override void Load ()
		{
            ToggleKey = RegisterHotKey("Quick Toggle Laser", "P");
			ToggleAimKey = RegisterHotKey("Quick Toggle Aim Laser", "T");
			AimKey = RegisterHotKey("Quick Aim Laser", "Y");
		}
		public override void Unload() {
			AimKey = null;
			ToggleKey = null;
			ToggleAimKey = null;
		}
	}
	
	public class LaserToggle : ModPlayer
	{
		public static bool Aim;
		public static bool AutoAim = false;
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
				CombatText.NewText(player.getRect(),(AutoAim ? Color.LightGreen : Color.Pink),(AutoAim ? "Aim Lock Enabled" : "Aim Lock Disabled"));
			}
			if (LaserSightNew.AimKey.Current || AutoAim) {
				if (LaserSight.hitmark != -1) {
					NPC npc = Main.npc[LaserSight.hitmark];
					if (npc.active) {
						Aim = true;
						Main.mouseX = (int)(npc.Center.X - Main.screenPosition.X);
						Main.mouseY = (int)(npc.Center.Y - Main.screenPosition.Y);
					}
				}
			}
		}
	}
	
	class LaserSight : ModWorld
	{
		//hitmark , the index of targeted enemy
		public static int hitmark;
		public override void PostDrawTiles() {
			if (LaserToggle.Aim) {
				if (hitmark != -1) {
					NPC npc = Main.npc[hitmark];
					if (npc.active) {
						Main.mouseX = (int)(npc.Center.X - Main.screenPosition.X);
						Main.mouseY = (int)(npc.Center.Y - Main.screenPosition.Y);
					}
				}
			}
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

					// color setup
					Color color = (LaserConfig.get.laserDisco ? Main.DiscoColor : LaserConfig.get.laserColor);

					// if npc is gone , set hitmark to -1 
					if (hitmark > -1 && !Main.npc[hitmark].active) {hitmark = -1;}

					// only accept valid index
					if (LaserConfig.get.laserEnemy && hitmark > -1) {

						//the npc
						NPC npc = Main.npc[hitmark];

						//start
						Main.spriteBatch.Begin (SpriteSortMode.Deferred, null, null, null, null, null, Main.GameViewMatrix.ZoomMatrix);

						//texture
						var text = ModContent.GetTexture("LaserSightNew/Cross");

						//do some sinwave scale crap
						float scale = (float)Math.Sin(Main.GameUpdateCount/60f);
						if (scale < 0f) {scale *= -1f;}
						scale += 0.3f;

						//draw the hitmark
						Main.spriteBatch.Draw(text, npc.Center - Main.screenPosition, null, color, 
						MathHelper.ToRadians(Main.GameUpdateCount)*1.5f, text.Size()/2f, scale, SpriteEffects.None, 0);

						//end
						Main.spriteBatch.End();
					}

					// start with blendstate.additive abuse
					Main.spriteBatch.Begin (SpriteSortMode.Deferred, BlendState.Additive, null, null, null, null, Main.GameViewMatrix.ZoomMatrix);

					//setup some variable
					Vector2 endPoint = player.Center;
					Vector2 laserStart = player.Center;
					float LaserRotation = (Main.MouseWorld - laserStart).ToRotation();
					Vector2 offset = new Vector2(5,5);

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
					int b = -1;
					for (int i = 0; i < Main.maxNPCs; i++)
					{
						NPC npc = Main.npc[i];
						if (npc.active) {
							float between = Vector2.Distance(npc.Center, laserStart);
							bool closest = Vector2.Distance(laserStart, endPoint) > between;
							var targetHitbox = npc.Hitbox;
							float point = 0f;
							// let collison do all the magic , honestly i dont even know how does this work without destroying frame rate
							bool collide = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), laserStart,endPoint, 5, ref point);
							if ((closest || b == -1 ) && collide) {
								b = i;
								endPoint = npc.Center;
								if (!LaserToggle.Aim) {
									hitmark = i;
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
					Main.spriteBatch.Draw(texBeam, target, null, color, LaserRotation, origin, 0, 0);

					//lighting
					if (LaserConfig.get.laserLight) {
						for (int i = 0; i < width; i += 10) {
							Lighting.AddLight(pos + Vector2.UnitX.RotatedBy(LaserRotation) * i + Main.screenPosition, color.ToVector3() * height * 0.030f);         
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
		// save the config
		public static void SaveConfig(){
			typeof(ConfigManager).GetMethod("Save", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[1] { get });
		}

		public override ConfigScope Mode => ConfigScope.ClientSide;
		public static LaserConfig get => ModContent.GetInstance<LaserConfig>();
		
		[Header("Visuals")]
		
		[Label("Laser Enabled")]
		[Tooltip("Whether a laser appears or not")]
		[DefaultValue(true)]
		public bool laserEnabled;

		[Label("Laser Distance Limit to mouse")]
		[Tooltip("Limit laser length to mouse\nthis will ignore 'Laser Distance Limit'")]
		[DefaultValue(false)]
		public bool laserMouse;

		[Label("Laser Distance Limit")]
		[Tooltip("The distance limit of the laser")]
		[Range(30, 500)]
		[Increment(10)]
		[DefaultValue(200)]
		[Slider] 
		public int laserLimit;

		[Label("Laser Scale")]
		[Tooltip("Change how thin or w i d e the laser could get")]
		[Range(1, 20)]
		[Increment(1)]
		[DefaultValue(10)]
		[Slider] 
		public int scale;

		[Label("Laser Auto Scale")]
		[Tooltip("Laser width will be based on your camera zoom\n this will ignore 'Laser Scale'")]
		[DefaultValue(false)]
		public bool scaleAuto;

		[Label("Laser Enemy Indicator")]
		[Tooltip("Show a mark if the laser detect an enemy\nuse one of the hotkey to aim at the enemy at all time")]
		[DefaultValue(true)]
		public bool laserEnemy;

		[Label("Laser Light")]
		[Tooltip("Make laser emmit light")]
		[DefaultValue(true)]
		public bool laserLight;

		[Label("Show laser in any weapons or unarmed")]
		[Tooltip("Allows laser to be shown in any weapons other than ranged weapon, including unarmed")]
		[DefaultValue(false)]
		public bool anyWeap;

		[Label("Show laser in only when using sniper scope")]
		[Tooltip("Show laser only when player using a sniper scope")]
		[DefaultValue(false)]
		public bool laserScope;

		[Label("Laser Color")]
		[Tooltip("The color of the laser")]
		[DefaultValue(typeof(Color), "255,100,0,255")]
		public Color laserColor = new Color(255,100,0,255);

		[Label("Laser Disco Color")]
		[Tooltip("Cycle between colors \ndoes not include amongus color")]
		[DefaultValue(false)]
		public bool laserDisco;
	}
	
	/*public class hitmarkerColorData
	{
		[Header("Laser Color")]

		[Label("Laser Color")]
		[DefaultValue(typeof(Color), "255,0,0,255")]
		public Color LaserConfig.get.laserColor = new Color(255,0,0,255);
	}*/
}