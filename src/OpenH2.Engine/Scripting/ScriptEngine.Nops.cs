﻿using OpenH2.Core.Scripting;
using OpenH2.Core.Tags.Scenario;
using System.Threading.Tasks;

namespace OpenH2.Engine.Scripting
{
    public partial class ScriptEngine : IScriptEngine
    {

        /// <summary>call this to force texture and geometry cache to block until satiated</summary>
        public Task cache_block_for_one_frame()
        {
            return Task.CompletedTask;
        }

        /// <summary>predict resources at a frame in camera animation.</summary>
        public void camera_predict_resources_at_frame(Animation animation, string /*id*/ emotion, Unit unit, ScenarioTag.LocationFlagDefinition locationFlag, int intValue)
        {
        }

        /// <summary>predict resources given a camera point</summary>
        public void camera_predict_resources_at_point(ScenarioTag.CameraPathTarget cutscene_camera_point)
        {
        }

        /// <summary>sets the mission segment for single player data mine events</summary>
        public void data_mine_set_mission_segment(string value)
        {
        }


        public void disable_render_light_suppressor()
        {
        }

        /// <summary>causes all garbage objects except those visible to a player to be collected immediately</summary>
        public void garbage_collect_now()
        {
        }

        /// <summary>forces all garbage objects to be collected immediately, even those visible to a player (dangerous!)</summary>
        public void garbage_collect_unsafe()
        {
        }

        /// <summary>we fear change</summary>
        public void geometry_cache_flush()
        {
        }

        /// <summary>loads textures necessary to draw an object that's about to come on-screen.</summary>
        public void object_type_predict(Entity entity)
        {
        }

        /// <summary>loads textures necessary to draw an object that's about to come on-screen.</summary>
        public void object_type_predict_high(Entity entity)
        {
        }

        /// <summary>loads textures/geometry/sounds necessary to present objects that are about to come on-screen</summary>
        public void objects_predict(EntityList object_list)
        {
        }

        /// <summary>loads textures/geometry/sounds necessary to present objects that are about to come on-screen</summary>
        public void objects_predict(Entity entity)
        {
        }

        /// <summary>loads textures/geometry/sounds necessary to present objects that are about to come on-screen</summary>
        public void objects_predict_high(Entity entity)
        {
        }

        /// <summary>predict a geometry block.</summary>
        public void predict_model_section(Model render_model, int value)
        {
        }

        /// <summary>predict a geometry block.</summary>
        public void predict_structure_section(Bsp structure_bsp, int value, bool boolean)
        {
        }

        /// <summary>removes the special place that activates everything it sees.</summary>
        public void pvs_clear()
        {
        }

        /// <summary>sets the specified object as the special place that activates everything it sees.</summary>
        public void pvs_set_object(Entity entity)
        {
        }


        /// <summary>enable</summary>
        public void rasterizer_bloom_override(bool boolean)
        {
        }

        /// <summary>brightness</summary>
        public void rasterizer_bloom_override_brightness(float real)
        {
        }

        /// <summary>threshold</summary>
        public void rasterizer_bloom_override_threshold(float real)
        {
        }

        /// <summary>enable/disable the specified unit to receive cinematic shadows where the shadow is focused about a radius around a marker name</summary>
        public void render_lights_enable_cinematic_shadow(bool boolean, Entity entity, string /*id*/ string_id, float real)
        {
        }

        /// <summary>your mom part 2.</summary>
        public void sound_impulse_predict(ReferenceGet soundRef)
        {
        }


        /// <summary>don't make me kick your ass</summary>
        public void texture_cache_flush()
        {
        }

        /// <summary>resets the time code timer</summary>
        public void time_code_reset()
        {
        }

        /// <summary>shows the time code timer</summary>
        public void time_code_show(bool boolean)
        {
        }

        /// <summary>starts/stops the time code timer</summary>
        public void time_code_start(bool boolean)
        {
        }
    }
}
