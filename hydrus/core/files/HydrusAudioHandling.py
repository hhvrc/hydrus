import re

from hydrus.core.files import HydrusFFMPEGParsing

def ParseFFMPEGAudio( lines ):
    
    audio_lines = HydrusFFMPEGParsing.GetAudioStreamLines( lines )
    
    audio_found = len( audio_lines ) > 0
    audio_format = 'unknown'
    
    audio_stream_mapping = '0:0'
    image_stream_mapping = None
    
    if audio_found:
        
        audio_line = audio_lines[0]
        
        try:
            
            match = re.search(" [0-9]* Hz", audio_line )
            
            audio_fps = int( audio_line[match.start()+1:match.end()] )
            
        except Exception as e:
            
            audio_fps = 'unknown'
            
        
        try:
            
            match = re.search( r'(?<=Audio:\s).+?(?=,)', audio_line )
            
            audio_format = match.group()
            
        except Exception as e:
            
            audio_format = 'unknown'
            
        
        try:
            
            audio_stream_mapping = HydrusFFMPEGParsing.ParseStreamMappingFromStreamLine( audio_line )
            
        except Exception as e:
            
            pass # default to 0:0
            
        
        image_lines = HydrusFFMPEGParsing.GetImageStreamLines( lines )
        
        if len( image_lines ) > 0:
            
            image_line = image_lines[0]
            
            try:
                
                image_stream_mapping = HydrusFFMPEGParsing.ParseStreamMappingFromStreamLine( image_line )
                
            except Exception as e:
                
                pass # default to None, no worries
                
            
        
    
    return ( audio_found, audio_format, audio_stream_mapping, image_stream_mapping )
    
