#version 300 es
precision mediump float;
in vec3 normal; in vec2 texcoord; uniform sampler2D texture0; uniform vec4 colDiffuse; uniform float uPulse;
out vec4 finalColor;
void main(){float light=0.28+0.72*max(dot(normalize(normal),normalize(vec3(-0.4,0.8,0.5))),0.0);finalColor=vec4(colDiffuse.rgb*light*uPulse,1.0);}
