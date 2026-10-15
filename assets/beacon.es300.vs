#version 300 es
precision mediump float;
in vec3 vertexPosition; in vec3 vertexNormal; in vec2 vertexTexCoord;
uniform mat4 mvp; uniform mat4 matNormal;
out vec3 normal; out vec2 texcoord;
void main(){normal=normalize(mat3(matNormal)*vertexNormal);texcoord=vertexTexCoord;gl_Position=mvp*vec4(vertexPosition,1.0);}
