#version 120


struct PointLight {
    vec3 position;

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;

    float constant;
    float linear;
    float quadratic;
};


struct SpotLight {
    vec3 position;
    vec3 direction;
    float inner_angle;
    float outer_angle;

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;

    float constant;
    float linear;
    float quadratic;
};


struct Material {
    sampler2D diffuse;
    sampler2D specular;
    sampler2D emission;
    float shininess;
};


# define NUM_POINT_LIGHTS 4



// IN
varying vec3 pass_world_position;
varying vec3 pass_normal;
varying vec2 pass_texture_coordinate;


// UPLOADED
uniform mat4 view;                          // For extracting camera position.
uniform PointLight light[NUM_POINT_LIGHTS];
uniform Material material;
uniform float time;


// FUNCTIONS
float attenuation(SpotLight light, float distance);
float attenuation(PointLight light, float distance);
vec3 ambient(PointLight light, Material material, vec2 coordinate);
vec3 diffuse(PointLight light, Material material, vec2 coordinate, vec3 normal, vec3 position);
vec3 specular(PointLight light, Material material, vec2 coordinate, vec3 normal, vec3 position, vec3 camera_position);


void main()
{
    vec3 normal_unit = normalize(pass_normal);
    vec3 camera_position = -view[3].xyz;         // Camera is the inverted view location.


    vec3 ambient  = vec3(0.0);
    vec3 diffuse  = vec3(0.0);
    vec3 specular = vec3(0.0);

    for (int i = 0; i < NUM_POINT_LIGHTS; i++)
    {
        float distance = length(light[i].position - pass_world_position);
        float attenuation = attenuation(light[i], distance);

        ambient += ambient(light[i], material, pass_texture_coordinate) * attenuation;
        diffuse += diffuse(light[i], material, pass_texture_coordinate, normal_unit, pass_world_position) * attenuation;
        specular += specular(light[i], material, pass_texture_coordinate, normal_unit, pass_world_position, camera_position) * attenuation;
    }

    gl_FragColor =  vec4(specular + diffuse + ambient, 1.0);

}


float attenuation(SpotLight light, vec3 position, float distance)
{
    vec3 direction_to_vertex = normalize(position - light.position);
    float angle = acos(dot(direction_to_vertex, light.direction));

    if (angle < light.inner_angle)
    {
        return 1.0 / (light.constant + light.linear * distance + light.quadratic * distance * distance);
    }
    else if (angle >= light.outer_angle)
    {
        return 0;
    }
    else
    {
        float attenuation = 1.0 / (light.constant + light.linear * distance + light.quadratic * distance * distance);
        float factor = 1 - (angle - light.inner_angle) / (light.outer_angle - light.inner_angle);
        return attenuation * factor;
    }

}


float attenuation(PointLight light, float distance)
{
    return 1.0 / (light.constant + light.linear * distance + light.quadratic * distance * distance);
}

vec3 ambient(PointLight light, Material material, vec2 coordinate)
{
    return light.ambient * vec3(texture2D(material.diffuse, coordinate));
}

vec3 diffuse(PointLight light, Material material, vec2 coordinate, vec3 normal, vec3 position)
{
    vec3 light_direction = normalize(light.position - position);
    float diffuse_factor = max(dot(normal, light_direction), 0.0);
    return light.diffuse * diffuse_factor * vec3(texture2D(material.diffuse, coordinate));
}

vec3 specular(PointLight light, Material material, vec2 coordinate, vec3 normal, vec3 position, vec3 camera_position)
{
    vec3 light_direction = normalize(light.position - position);
    vec3 camera_direction = normalize(camera_position - position);
    vec3 reflection_direction = reflect(-light_direction, normal);
    float specular_factor = pow(max(dot(camera_direction, reflection_direction), 0.0), material.shininess);
    return light.specular * specular_factor * vec3(texture2D(material.specular, pass_texture_coordinate));
}