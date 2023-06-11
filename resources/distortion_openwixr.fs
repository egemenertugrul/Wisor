#version 330

in vec2 fragTexCoord;
in vec4 fragColor;

uniform sampler2D texture0;

uniform vec2 _offset;
uniform float _distortion;
uniform float _cubicDistortion;
uniform bool _isRight;
uniform float _scale;
uniform vec4 _OutOfBoundColour;

uniform vec2 leftScreenCenter = vec2(0.25, 0.5);
uniform vec2 rightScreenCenter = vec2(0.75, 0.5);

out vec4 finalColor;

vec2 barrel(vec2 uv) {
    vec2 h;
    vec2 off = _offset;

    vec2 center;
    if(uv.x < 0.5f){
        center = leftScreenCenter;
        off.x *= 1;
    } else {
        center = rightScreenCenter;
    }
    h = uv - center;
    h += off;

    float r2 = h.x * h.x;
    r2 *= 3.0;

    float f = 1.0 + r2 * (_distortion + _cubicDistortion * r2);
    float dec = f * center.y;
    vec2 ret = vec2(f * h.x + center.x, h.y + dec);

    if (ret.x < 0 || ret.x > 1.0 || ret.x > center.x * 2 || ret.x < center.x - leftScreenCenter.x 
    || ret.y < 0.0 || ret.y > 1.0) {
        return vec2(-5.0, -5.0); //uv out of bound so display out of bound color
    }

    return ret;
}

void main() {
    vec2 distortedUV = barrel(fragTexCoord);

    if (distortedUV.x < -1.0 && distortedUV.y < -1.0) {
        finalColor = _OutOfBoundColour;
    } else {
        vec4 distorted = texture2D(texture0, distortedUV);
        finalColor = distorted;
    }
}
