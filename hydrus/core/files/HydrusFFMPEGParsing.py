import re

from hydrus.core import HydrusExceptions

def GetAudioStreamLines( lines: list[ str ] ):
    
    # the ^\sStream is to exclude the 'title' line, when it exists, includes the string 'Audio: ', ha ha
    # Stream #0:0: Audio: mp3 (mp3float), 44100 Hz, stereo, fltp, 192 kb/s
    audio_lines = [ line for line in lines if line.startswith( 'Stream ' ) and 'Audio: ' in line ]
    
    return audio_lines
    

def GetImageStreamLines( lines: list[ str ] ):
    
    video_lines = GetVideoStreamLines( lines )
    
    def looks_like_image_stream( line ):
        
        return 'Video: png' in line or 'Video: jpg' in line
        
    
    def looks_like_attached_pic( line ):
        
        return 'attached pic' in line or 'attached_pic' in line
        
    
    # Stream #0:1: Video: png, rgb24(pc, gbr/unknown/unknown), 300x300 [SAR 2835:2835 DAR 1:1], 90k tbr, 90k tbn (attached pic)
    image_lines = [ line for line in video_lines if looks_like_image_stream( line ) or looks_like_attached_pic( line ) ]
    
    return image_lines
    

def GetVideoStreamLines( lines: list[ str ] ):
    """
    This returns single-frame pngs and stuff!
    """
    
    # the "startswith Stream" check is to exclude the 'title' line, when it exists, includes the string 'Audio: ', ha ha
    # Stream #0:0[0x1]: Video: av1 (libdav1d) (Professional) (av01 / 0x31307661), yuv444p12le(pc), 159x159, 1 fps, 1 tbr, 1 tbn (default)
    # Stream #0:1: Video: png, rgb24(pc, gbr/unknown/unknown), 300x300 [SAR 2835:2835 DAR 1:1], 90k tbr, 90k tbn (attached pic)
    audio_lines = [ line for line in lines if line.startswith( 'Stream ' ) and 'Video: ' in line ]
    
    return audio_lines
    

def ParseStreamMappingFromStreamLine( line: str ):
    
    try:
        
        # Stream #0:1[0x1](eng): Video: hevc (Main) (hvc1 / 0x31637668), yuv420p(tv), 256x144, 711 kb/s, 25 fps, 25 tbr, 1k tbn (default)
        # Stream #0:0: Audio: mp3 (mp3float), 44100 Hz, stereo, fltp, 192 kb/s
        # Stream #0:1: Video: png, rgb24(pc, gbr/unknown/unknown), 300x300 [SAR 2835:2835 DAR 1:1], 90k tbr, 90k tbn (attached pic)
        
        match = re.search( r'(?<=^Stream #)\d+:\d+', line )
        
        if match is not None:
            
            stream_mapping = match.group()
            
            return stream_mapping
            
        
    except Exception as e:
        
        pass
        
    
    raise HydrusExceptions.DataMissing( 'No stream mapping found!' )
    
