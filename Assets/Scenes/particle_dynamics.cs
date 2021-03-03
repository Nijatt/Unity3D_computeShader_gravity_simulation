//=============================================================================
// GRAVITATIONAL SIMULATION
//=============================================================================


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;



public class particle_dynamics : MonoBehaviour
{

    public static int total_particle_instances = 10000;  // 16000000;
    public static int sub_particle_instances = 1000;//160000;
    public Vector3 particle_max_pos_range;
    public Vector3 particle_max_vel_range;
    public Vector3 particle_max_force_range;
    public float particle_mass;
    float particle_scale=1f;

    //rendering
    public Mesh particle_mesh;
    public Material particle_material;


    //GPU computing
    public ComputeShader shader;

    struct Particle
    {
        //kinematics
        public Vector3 position_vector { get; set; }
        public Vector3 velocity_vector { get; set; }
        public Vector3 force_vector { get; set; }
        public float mass { get; set; }

        // rotation and scale
        public static Vector3 particle_scale = new Vector3(1f,1f,1f);
        public static Quaternion rotation = Quaternion.identity;

        public Matrix4x4 matrix
        {
            get
            {
                return Matrix4x4.TRS(position_vector, rotation, particle_scale);
            }
        }



        //Constructor
        public Particle(Vector3 Cposition_vector, Vector3 Cvelocity_vector, Vector3 Cforce_vector, float Cmass)
        {
            position_vector = Cposition_vector;
            velocity_vector = Cvelocity_vector;
            force_vector = Cforce_vector;
            mass = Cmass;
        }
    }


    List<Particle[]> input_data = new List<Particle[]>();
  
    Particle create_random_particle() {
        // just creates particle randomly
        Particle particle_data = new Particle();
        particle_data.position_vector = new Vector3(Random.Range(-particle_max_pos_range.x, particle_max_pos_range.x), Random.Range(-particle_max_pos_range.y, particle_max_pos_range.y), Random.Range(-particle_max_pos_range.z, particle_max_pos_range.z));
        particle_data.velocity_vector = new Vector3(Random.Range(-particle_max_vel_range.x, particle_max_vel_range.x), Random.Range(-particle_max_vel_range.y, particle_max_vel_range.y), Random.Range(-particle_max_vel_range.z, particle_max_vel_range.z));
        particle_data.force_vector = new Vector3(Random.Range(-particle_max_force_range.x, particle_max_force_range.x), Random.Range(-particle_max_force_range.y, particle_max_force_range.y), Random.Range(-particle_max_force_range.z, particle_max_force_range.z));
        particle_data.mass = Random.Range(1,particle_mass);
        return particle_data;
    }

    

    void create_random_particles() {
        // just creates particles randomly
        for (int j = 0; j < total_particle_instances/ sub_particle_instances; j++)
        {
            Particle[] sub_input_data = new Particle[sub_particle_instances];
            for (int i = 0; i < sub_particle_instances; i++)
            {
                sub_input_data[i] = create_random_particle();
            }
            input_data.Add(sub_input_data);
        }
    }


    void gravity_correction()
    {
        Particle[] data_gravity_correction = new Particle[input_data.Count];
        for (int j = 0; j < input_data.Count; j++)
        {
            data_gravity_correction[j] = input_data[j][0];
        }
        ComputeBuffer buffer = new ComputeBuffer(data_gravity_correction.Length, 40);
        buffer.SetData(data_gravity_correction);
        int kernel = shader.FindKernel("CSMain");
        shader.SetBuffer(kernel, "particleBuffer", buffer);
        shader.Dispatch(kernel, data_gravity_correction.Length, 1, 1);
        buffer.GetData(data_gravity_correction);
        for (int j = 0; j < input_data.Count; j++)
        {
            input_data[j][0] = data_gravity_correction[j] ;
        }
        buffer.Dispose();
    }

    void GPU_solver()
    {
        for (int i = 0; i < input_data.Count; i++)
        {
            ComputeBuffer buffer = new ComputeBuffer(input_data[i].Length, 40);
            buffer.SetData(input_data[i]);
            int kernel = shader.FindKernel("CSMain");
            shader.SetBuffer(kernel, "particleBuffer", buffer);
            shader.Dispatch(kernel, input_data[i].Length, 1, 1);
            buffer.GetData(input_data[i]);
            buffer.Dispose();
        }
        gravity_correction();
    }



    void print_all_data()
    {
        for (int j = 0; j < input_data.Count; j++)
        {
            for (int i = 0; i < input_data[j].Length; i++)
            {
                Particle data = input_data[j][i];

                Debug.Log($"particle # [{i},{j}]");
                Debug.Log($"pos  : {data.position_vector}");
                Debug.Log($"vel  : {data.velocity_vector}");
                Debug.Log($"for  : {data.force_vector}");

            }
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        create_random_particles();
    }


    // Update is called once per frame
    void Update()
    {
        GPU_solver();
        RenderBatches();
       
    }

    private void PrimitiveRender()
        // this is just for an emergency use.
    {
            for (int j = 0; j < input_data.Count; j++)
            {
                for (int i = 0; i < input_data[j].Length; i++)
                {   
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.position = input_data[j][i].position_vector;
                }
            }
    }




    private void RenderBatches()
    {
        // render particle objects
        foreach (var batch in input_data)
        {
            Graphics.DrawMeshInstanced(particle_mesh, 0, particle_material, batch.Select((a) => a.matrix).ToList());
        }
    }
}
