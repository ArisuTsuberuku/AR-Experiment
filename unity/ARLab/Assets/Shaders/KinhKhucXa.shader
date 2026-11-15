// Kính thấu kính mỏng: nhìn qua kính thấy hình nền (camera AR + cảnh) bị phóng / lật đúng theo tiêu cự.
// Tia từ mắt qua điểm cách quang tâm r bị lệch góc r/f. Với L = mắt → kính, D = kính → nền phía sau,
// điểm nền nhìn thấy ứng với điểm màn hình co về quang tâm theo hệ số k = 1 − D·L / (f·(L + D)):
//   k ∈ (0, 1): ảnh ảo cùng chiều, phóng to 1/k (vật gần kính, D < f khi mắt ở xa);
//   k < 0     : ảnh ngược chiều (vật xa hơn tiêu cự).
// Giới hạn: lấy một D cho cả mặt kính, không làm nhoè, không tán sắc.
Shader "ARLab/KinhKhucXa"
{
    Properties
    {
        _MauKinh ("Màu kính (a = độ phủ)", Color) = (0.85, 0.95, 1, 0.03)
        _TieuCu ("Tiêu cự (m)", Float) = 0.2
        _KhoangCachNen ("Kính tới nền phía sau (m)", Float) = 1
        _TamQuangWS ("Quang tâm (thế giới)", Vector) = (0, 0, 0, 0)
        _DoLoa ("Độ lóa viền", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "KhucXa"
            Tags { "LightMode" = "UniversalForward" }
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _MauKinh;
                float _TieuCu;
                float _KhoangCachNen;
                float4 _TamQuangWS;
                half _DoLoa;
            CBUFFER_END

            struct VaoDinh { float4 viTri : POSITION; float3 phapTuyen : NORMAL; };
            struct RaDinh
            {
                float4 viTriCS : SV_POSITION;
                float4 manHinh : TEXCOORD0;
                float3 phapTuyenWS : TEXCOORD1;
                float3 viTriWS : TEXCOORD2;
            };

            RaDinh vert(VaoDinh i)
            {
                RaDinh o;
                o.viTriWS = TransformObjectToWorld(i.viTri.xyz);
                o.viTriCS = TransformWorldToHClip(o.viTriWS);
                o.manHinh = ComputeScreenPos(o.viTriCS);
                o.phapTuyenWS = TransformObjectToWorldNormal(i.phapTuyen);
                return o;
            }

            half4 frag(RaDinh i) : SV_Target
            {
                float2 uv = i.manHinh.xy / i.manHinh.w;
                float4 tam = ComputeScreenPos(TransformWorldToHClip(_TamQuangWS.xyz));
                float2 uvTam = tam.xy / tam.w;

                float3 mat = GetCameraPositionWS();
                float L = max(distance(mat, _TamQuangWS.xyz), 0.01);
                float D = max(_KhoangCachNen, 0.0);
                float k = 1.0 - D * L / (max(_TieuCu, 1e-4) * (L + D));

                float2 uvNen = uvTam + (uv - uvTam) * k;
                half3 nen = SampleSceneColor(saturate(uvNen));
                // ngoài khung hình thì không có dữ liệu: làm tối dần thay vì kéo dãn mép ảnh
                float2 ngoai = max(-uvNen, uvNen - 1.0);
                nen *= 1.0 - saturate(max(ngoai.x, ngoai.y) * 8.0);

                float3 V = normalize(mat - i.viTriWS);
                half loa = pow(1.0 - saturate(abs(dot(normalize(i.phapTuyenWS), V))), 4.0) * _DoLoa;
                half3 mau = lerp(nen, _MauKinh.rgb, _MauKinh.a) + loa;
                return half4(mau, 1.0);
            }
            ENDHLSL
        }
    }
}
